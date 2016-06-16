namespace Xamarin.Forms.FSharp

open System

open Xamarin.Forms

type Var<'T> =
    {
        mutable value: 'T
        callbacks: ResizeArray<'T -> unit>
    }

    member this.Value = this.value

    member this.SetValue value =
        this.value <- value
        this.Notify ()

    member this.Update f =
        this.value <- f this.value
        this.Notify ()

    member this.Notify () =
        for i = 0 to this.callbacks.Count - 1 do
            let f = this.callbacks.[i]
            f this.value

type Val<'T> =
    {
        mutable value: 'T
        callbacks: ResizeArray<'T -> unit>
        subscriptions: ResizeArray<IDisposable>
    }

    member this.Value = this.value

    member this.Notify () =
        for i = 0 to this.callbacks.Count - 1 do
            let f = this.callbacks.[i]
            f this.value

    member this.Subscribe (callback: 'T -> unit) =
        callback this.value
        this.callbacks.Add callback
        {
            new IDisposable with

                member __.Dispose () =
                    this.callbacks.Remove (callback) |> ignore
        }

[<RequireQualifiedAccess>]
module Var =

    let create initialValue : Var<_> =
        {
            value = initialValue
            callbacks = ResizeArray ()
        }

[<RequireQualifiedAccess>]
module Val =

    let constant value : Val<_> =
        {
            value = value
            callbacks = ResizeArray ()
            subscriptions = ResizeArray ()
        }

    let ofVar (var: Var<'T>) : Val<'T> =
        {
            value = var.Value
            callbacks = var.callbacks
            subscriptions = ResizeArray ()
        }

    let map f (va: Val<_>) =
        let newVa = constant Unchecked.defaultof<_>

        newVa.subscriptions.Add (
            va.Subscribe (fun value ->
                newVa.value <- f value
                newVa.Notify ()
            )
        )

        newVa

    let map2 f (va1: Val<_>) (va2: Val<_>) =
        let newVa = constant Unchecked.defaultof<_>
        let mutable hasLastSubscription = false

        newVa.subscriptions.Add (
            va1.Subscribe (fun () ->
                if hasLastSubscription then 
                    newVa.value <- f va1.value va2.value
                    newVa.Notify ()
            )
        )

        newVa.subscriptions.Add (
            va2.Subscribe (fun () ->
                newVa.value <- f va1.value va2.value
                newVa.Notify ()
                hasLastSubscription <- true
            )
        )

        newVa



type FormsContext =
    {
        Disposables: IDisposable ResizeArray
    }

    member this.AddDisposable x =
        this.Disposables.Add x

type ElementAttribute<'T when 'T :> View> = 
    | Once of ('T -> unit)
    | Dynamic of (FormsContext -> 'T -> unit)

[<AutoOpen>]
module ElementAttributes =

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
                va.Subscribe (fun value -> (^T : (member set_Text : string -> unit) (el, value)))
                |> context.AddDisposable
            )

    let inline textBinding< ^T when ^T : (member set_Text : string -> unit) and
                        ^T : (member get_Text : unit -> string) and
                        ^T : (member add_TextChanged : System.EventHandler<TextChangedEventArgs> -> unit) and
                        ^T :> View> (var: Var<string>) =
        Dynamic (fun context (el: ^T) ->
            ()
            //let handler = (^T : (member add_TextChanged : System.EventHandler<TextChangedEventArgs> -> unit) (el))

            //var.Subscribe (fun () -> (^T : (member set_Text : string -> unit) (el, var.Value)))
        )

[<AbstractClass>]
type BaseView () =

    abstract CreateView : FormsContext -> View

    abstract SubscribeView : FormsContext -> View -> unit

type View<'T when 'T :> View> (create: FormsContext -> 'T, subscribe: FormsContext -> 'T -> unit) =
    inherit BaseView ()

    member this.Create = create

    member this.Subscribe = subscribe
        
    override this.CreateView context = (create context) :> View

    override this.SubscribeView context view = subscribe context (view :?> 'T)

[<AutoOpen>]
module Helpers =

    let extend create subscribe (view: View<'T>) =
        View<'T> (
            (fun context ->
                let view' : 'T = view.Create context

                create context view'

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

    let attributes (attribs: ElementAttribute<_> list) (view: View<_>) =
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

    //module Dynamic =

    //    let children (children: Val<BaseView list>) (view: View<AbsoluteLayout>) =
    //        let childrenViews = ResizeArray<WeakReference<View>> ()

    //        view
    //        |> extend 
    //            (fun context view ->
    //                ()
    //            )

    //            (fun context view' ->

    //                children.Subscribe (fun children ->
    //                    childrenViews
    //                    |> Seq.iter (fun childView ->
    //                        match childView.TryGetTarget () with
    //                        | (true, childView) -> view'.Children.Remove (childView) |> ignore
    //                        | _ -> ()
    //                    )

    //                    children
    //                    |> Seq.iter (fun child ->
    //                        let childView = child.CreateView context
    //                        childrenViews.Add(WeakReference<View> (childView))
    //                        view'.Children.Add (childView)
    //                    )
    //                )
    //                |> context.AddDisposable
    //            )

type FSharpContentPage (view: BaseView) =
    inherit ContentPage ()

    let mutable isInitialized = false
    let mutable context = 
        {
            Disposables = ResizeArray ()
        }

    override this.OnAppearing () =
        base.OnAppearing ()

        if not isInitialized then
            this.Content <- view.CreateView context
            isInitialized <- true

        view.SubscribeView context this.Content

    override this.OnDisappearing () =
        base.OnDisappearing ()

        context.Disposables
        |> Seq.iter (fun x -> x.Dispose ())

        context.Disposables.Clear ()

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

    let toContentPage (view: View<_>) =
        FSharpContentPage (view)