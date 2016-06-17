namespace Xamarin.Forms.FSharp

open System

open Xamarin.Forms

type ViewAttribute<'T when 'T :> View> = 
    | Once of ('T -> unit)
    | Dynamic of (Context -> 'T -> unit)

[<AutoOpen>]
module ViewAttributes =

    let inline verticalOptions< ^T when ^T : (member set_VerticalOptions : LayoutOptions -> unit) and
                            ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_VerticalOptions : LayoutOptions -> unit) (el, value)))

    let inline horizontalOptions< ^T when ^T : (member set_HorizontalOptions : LayoutOptions -> unit) and
                            ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_HorizontalOptions : LayoutOptions -> unit) (el, value)))

    let inline text< ^T when ^T : (member set_Text : string -> unit) and
                        ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_Text : string -> unit) (el, value)))

    let inline widthRequest< ^T when ^T : (member set_WidthRequest : float -> unit) and
                        ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_WidthRequest : float -> unit) (el, value)))

    let inline heightRequest< ^T when ^T : (member set_HeightRequest : float -> unit) and
                        ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_HeightRequest : float -> unit) (el, value)))

    let inline source< ^T when ^T : (member set_Source : ImageSource -> unit) and
                        ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_Source : ImageSource -> unit) (el, value)))

    let inline aspect< ^T when ^T : (member set_Aspect : Aspect -> unit) and
                        ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_Aspect : Aspect -> unit) (el, value)))   

    let inline absoluteLayoutFlags< ^T when ^T :> View> value =
        Once (fun (el: ^T) -> AbsoluteLayout.SetLayoutFlags (el, value))   

    let inline absoluteLayoutBounds< ^T when ^T :> View> value =
        Once (fun (el: ^T) -> AbsoluteLayout.SetLayoutBounds (el, value))

    module Dynamic =

        let inline text< ^T when ^T : (member set_Text : string -> unit) and
                            ^T :> View> (va: Val<string>) =
            Dynamic (fun context (el: ^T) ->
                context.Subscribe (fun value -> (^T : (member set_Text : string -> unit) (el, value))) va
            )

    //let inline textBinding< ^T when ^T : (member set_Text : string -> unit) and
    //                    ^T : (member get_Text : unit -> string) and
    //                    ^T : (member add_TextChanged : System.EventHandler<TextChangedEventArgs> -> unit) and
    //                    ^T :> View> (var: Var<string>) =
    //    Dynamic (fun context (el: ^T) ->
    //        ()
    //        //let handler = (^T : (member add_TextChanged : System.EventHandler<TextChangedEventArgs> -> unit) (el))

    //        //var.Subscribe (fun () -> (^T : (member set_Text : string -> unit) (el, var.Value)))
    //    )

[<AbstractClass>]
type BaseView () =

    abstract CreateView : Context -> View

    abstract SubscribeView : Context -> View -> unit

type View<'T when 'T :> View> (create: Context -> 'T, subscribe: Context -> 'T -> unit) =
    inherit BaseView ()

    member this.Create = create

    member this.Subscribe = subscribe
        
    override this.CreateView context = (create context) :> View

    override this.SubscribeView context view = subscribe context (view :?> 'T)

[<AutoOpen>]
module View =

    let extend f subscribe (view: View<'T>) =
        View<'T> (
            (fun context ->
                let view' : 'T = view.Create context

                f context view'

                view'
            ),

            (fun context view' ->
                subscribe context view'
            )
        )

    let children (children: BaseView list) (view: View<AbsoluteLayout>) =
        let childrenViews = ResizeArray ()
        view
        |> extend 
            (fun context view ->
                children
                |> List.iter (fun child ->
                    let childView = child.CreateView context
                    view.Children.Add (childView)
                    childrenViews.Add (WeakReference<View> (childView))
                )
            )

            (fun context view' ->
                (children, childrenViews)
                ||> Seq.iter2 (fun child childView -> 
                    match childView.TryGetTarget () with
                    | (true, childView) -> child.SubscribeView context childView
                    | _ -> ()
                )
            )

    let attributes (attribs: ViewAttribute<_> list) (view: View<_>) =
        view
        |> extend
            (fun context view' ->
                attribs
                |> List.iter (function
                    | Once f -> f view'
                    | _ -> ()
                )
            )

            (fun context view' ->
                attribs
                |> List.iter (function
                    | Dynamic f -> f context view'
                    | _ -> ()
                )
            )

type FSharpContentPage (view: BaseView) =
    inherit ContentPage ()

    let mutable isInitialized = false
    let mutable context = Unchecked.defaultof<_>

    member val Subscribe : Context -> unit = fun _ -> () with get, set

    member val Appeared = Event<unit> () with get

    member val Disappeared = Event<unit> () with get

    override this.OnAppearing () =
        base.OnAppearing ()

        context <- new Context ()

        this.Subscribe context

        if not isInitialized then
            this.Content <- view.CreateView context
            isInitialized <- true
            context.WillForceImmediateUpdate <- true

        view.SubscribeView context this.Content
        this.Appeared.Trigger ()

    override this.OnDisappearing () =
        base.OnDisappearing ()

        this.Disappeared.Trigger ()

        (context :> IDisposable).Dispose ()
        context <- Unchecked.defaultof<_>

type Page<'T when 'T :> Page> =
    {
        Create: unit -> 'T
        Subscribe: 'T -> Context -> unit
    }

module Page =

    let build (page: Page<'T>) =
        let page' = page.Create ()
        match (page' :> obj) with
        | :? FSharpContentPage as page'' ->
            page''.Subscribe <- page.Subscribe page'
        | _ -> ()
        page'

    let onAppear (f: Async<unit>) (page: Page<FSharpContentPage>) =
        {
            Create = fun () ->
                let page' = page.Create ()

                page'

            Subscribe = fun page' context ->
                page'.Appeared.Publish.Subscribe (fun () ->
                    Async.StartImmediate f
                )
                |> context.AddDisposable
        }

    let navigationPush (value: Val<Page<_> option>) (page: Page<FSharpContentPage>) =
        {
            Create = fun () ->
                let page' = page.Create ()

                page'

            Subscribe = fun page' context ->
                page.Subscribe page' context

                value
                |> context.Subscribe (function
                    | Some page ->
                        page |> build
                        |> page'.Navigation.PushAsync
                        |> ignore
                    | _ -> ()
                )
        }

    let content view =
        {
            Create = fun () -> FSharpContentPage (view)
            Subscribe = fun _ _ -> ()
        }

    let navigation page =
        {
            Create = fun () -> NavigationPage (page.Create ())
            Subscribe = fun _ _ -> ()
        }

[<AutoOpen>]
module Views =

    let absoluteLayout attribs children' =
        View<AbsoluteLayout> (
            (fun _ ->
                let absoluteLayout = AbsoluteLayout ()

                absoluteLayout
            ),

            (fun context stackLayout ->
                ()
            )
        )
        |> attributes attribs
        |> children children'

    let image attribs =
        View<Image> (
            (fun _ ->
                let image = Image ()

                image
            ),

            (fun context image ->
                ()
            )
        )
        |> attributes attribs
