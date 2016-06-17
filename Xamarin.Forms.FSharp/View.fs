namespace Xamarin.Forms.FSharp

open System

open Xamarin.Forms

[<AbstractClass>]
type BaseView () =

    abstract CreateView : unit -> View

    abstract SubscribeView : Context -> View -> unit

type View<'T when 'T :> View> (create: unit -> 'T, subscribe: Context -> 'T -> unit) =
    inherit BaseView ()

    member this.Create = create

    member this.Subscribe = subscribe
        
    override this.CreateView () = (create ()) :> View

    override this.SubscribeView context view = subscribe context (view :?> 'T)

[<AutoOpen>]
module View =

    let create f subscribe =
        View<_> (f, subscribe)

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
                    let childView = child.CreateView ()
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
