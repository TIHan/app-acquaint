namespace Xamarin.Forms.FSharp

open System

open Sesame

open Xamarin.Forms

[<AbstractClass>]
type BaseView () =

    abstract Create : unit -> View

    abstract Subscribe : View -> Context -> unit

[<Sealed>]
type View<'T when 'T :> View> (create: unit -> 'T, subscribe: 'T -> Context -> unit) =
    inherit BaseView ()

    override this.Create () = create () :> View

    override this.Subscribe view context = subscribe (view :?> 'T) context

    member this.Extend (f, subscribe2) =
        View<'T> (
            (fun () ->
                let page = create ()
                f page
                page
            ),
            (fun page context ->
                subscribe page context
                subscribe2 page context 
            )
        )

type FSharpViewCell (view: BaseView) as this =
    inherit ViewCell ()

    let mutable context = new Context ()

    do
        this.View <- view.Create ()
        context.WillForceImmediateUpdate <- true

    member val Subscribe : Context -> unit = fun _ -> () with get, set

    override this.OnAppearing () =
        base.OnAppearing ()

        let isInitialized = context <> Unchecked.defaultof<_>

        if not isInitialized then
            context <- new Context ()

        this.Subscribe context

        if not isInitialized then
            context.WillForceImmediateUpdate <- true

        view.Subscribe this.View context

    override this.OnDisappearing () =
        base.OnDisappearing ()

        if (context <> Unchecked.defaultof<_>) then
            (context :> IDisposable).Dispose ()
            context <- Unchecked.defaultof<_>

type FSharpListItem =
    {
        View: BaseView
        Index: int
    }

type FSharpListView<'T when 'T :> BaseView> () as this =
    inherit ListView (ListViewCachingStrategy.RecycleElement)

    do
        this.ItemTemplate <- null

    member this.SubscribeItems (context: Context, va: Val<ResizeArray<'T>>) =
        va
        |> context.Subscribe (fun items ->
            this.ItemsSource <-
                items
                |> Seq.mapi (fun i x -> { View = x; Index = i })
        )

    override this.CreateDefault(o) =
        let item = o :?> FSharpListItem
        let cell = FSharpViewCell (item.View)
        cell :> Cell

[<RequireQualifiedAccess>]
module View =

    let create f subscribe =
        View<_> (f, subscribe)

    let extend f subscribe (view: View<'T>) =
        View<'T> (
            (fun () ->
                let view' : 'T = view.Create () :?> 'T

                f view'

                view'
            ),

            (fun context view' ->
                view.Subscribe context view'
                subscribe context view'
            )
        )

    let children (children: BaseView list) (view: View<AbsoluteLayout>) =
        let childrenViews = ResizeArray ()
        view
        |> extend 
            (fun view' ->
                children
                |> List.iter (fun child ->
                    let childView = child.Create ()
                    view'.Children.Add (childView)
                    childrenViews.Add (WeakReference<View> (childView))
                )
            )

            (fun view' context ->
                (children, childrenViews)
                ||> Seq.iter2 (fun child childView -> 
                    match childView.TryGetTarget () with
                    | (true, childView) -> child.Subscribe childView context
                    | _ -> ()
                )
            )

    let stackChildren (children: BaseView list) (view: View<StackLayout>) =
        let childrenViews = ResizeArray ()
        view
        |> extend 
            (fun view' ->
                children
                |> List.iter (fun child ->
                    let childView = child.Create ()
                    view'.Children.Add (childView)
                    childrenViews.Add (WeakReference<View> (childView))
                )
            )

            (fun view' context ->
                (children, childrenViews)
                ||> Seq.iter2 (fun child childView -> 
                    match childView.TryGetTarget () with
                    | (true, childView) -> child.Subscribe childView context
                    | _ -> ()
                )
            )

    let attributes (attribs: ViewAttribute<_> list) (view: View<_>) =
        view
        |> extend
            (fun view' ->
                attribs
                |> List.iter (function
                    | Once f -> f view'
                    | _ -> ()
                )
            )

            (fun view' context ->
                attribs
                |> List.iter (function
                    | Dynamic f -> f view' context
                    | _ -> ()
                )
            )

    let handlers (handlers: ViewHandler<_> list) (view: View<_>) =
        view
        |> extend
            (fun view' -> ())

            (fun view' context ->
                handlers
                |> List.iter (function
                    | Handler f -> f view' |> context.AddDisposable
                )
            )

[<AutoOpen>]
module Views =

    let absoluteLayout attribs children' =
        View.create
            (fun () -> AbsoluteLayout ())
            (fun _ _ -> ())
        |> View.attributes attribs
        |> View.children children'

    let stackLayout attribs children' =
        View.create
            (fun () -> StackLayout ())
            (fun _ _ -> ())
        |> View.attributes attribs
        |> View.stackChildren children'

    let image attribs =
        View.create
            (fun () -> Image ())
            (fun _ _ -> ())
        |> View.attributes attribs

    let button attribs onClick =
        View.create
            (fun () -> Button ())
            (fun _ _ -> ())
        |> View.attributes attribs
        |> View.handlers [ clicked onClick ]

    let entry attribs onTextChanged =
        View.create
            (fun () -> Entry ())
            (fun _ _ -> ())
        |> View.attributes attribs
        |> View.handlers [ textChanged onTextChanged ]

    let label attribs =
        View.create
            (fun () -> Label ())
            (fun _ _ -> ())
        |> View.attributes attribs

    let listView<'T when 'T :> BaseView> attribs (views: Val<ResizeArray<'T>>) =
        View.create
            (fun () -> FSharpListView ())
            (fun listView' context ->
                listView'.SubscribeItems (context, views)
            )
        |> View.attributes attribs
