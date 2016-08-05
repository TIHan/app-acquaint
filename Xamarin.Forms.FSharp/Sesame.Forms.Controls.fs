namespace FormsDSL

open System
open Sesame
open Xamarin.Forms

type FsStackLayout () =
    inherit StackLayout ()

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()

type FsAbsoluteLayout () =
    inherit StackLayout ()

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()

type FsLabel () =
    inherit Label ()

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()

type FsEntry () =
    inherit Entry ()

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()

type FsButton () =
    inherit Button ()

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()

type FsImage () =
    inherit Image ()

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()

type FsListView (app: WeakReference<Application>) =
    inherit ListView ()

    override this.CreateDefault (item: obj) =
        let lazyCell = (item :?> FormsDSL.Cell)
        lazyCell.Build (app)

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()


[<AutoOpen>]
module ViewComponentProperties =

    let inline verticalOptions< ^T when ^T : (member set_VerticalOptions : LayoutOptions -> unit) and
                            ^T :> View> value =
        Once (fun view' -> (^T : (member set_VerticalOptions : LayoutOptions -> unit) (view', value)))

    let inline horizontalOptions< ^T when ^T : (member set_HorizontalOptions : LayoutOptions -> unit) and
                            ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_HorizontalOptions : LayoutOptions -> unit) (el, value)))

    let inline text< ^T when ^T : (member set_Text : string -> unit) and
                        ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_Text : string -> unit) (el, value)))

    let inline xAlign< ^T when ^T : (member set_XAlign : TextAlignment -> unit) and
                        ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_XAlign : TextAlignment -> unit) (el, value)))

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

    let inline backgroundColor< ^T when ^T : (member set_BackgroundColor : Color -> unit) and
                        ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_BackgroundColor : Color -> unit) (el, value))) 

    let inline absoluteLayoutFlags< ^T when ^T :> View> value =
        Once (fun (el: ^T) -> AbsoluteLayout.SetLayoutFlags (el, value))   

    let inline absoluteLayoutBounds< ^T when ^T :> View> value =
        Once (fun (el: ^T) -> AbsoluteLayout.SetLayoutBounds (el, value))

    let inline children< ^T when 
                    ^T :> View and
                    ^T :> IView and  
                    ^T : (member get_Children : unit -> View System.Collections.Generic.IList)>
                        (children: ViewComponent list) =
        let childrenViews = ResizeArray ()

        OnceAndSubscribe (
            (fun context view ->
                children
                |> List.iter (fun child ->
                    let childView = context.CreateView child
                    ( ^T : (member get_Children : unit -> View System.Collections.Generic.IList) (view)).Add (childView)
                    childrenViews.Add (WeakReference<View> (childView))
                )
            ),
            (fun context view ->
                (children, childrenViews)
                ||> Seq.iter2 (fun child childView -> 
                    match childView.TryGetTarget () with
                    | (true, childView) -> context.SubscribeView child childView
                    | _ -> ()
                )

                (view :> obj :?> IView).InitializedEvent.Publish
                |> Observable.subscribe (fun () ->
                    childrenViews
                    |> Seq.iter (fun childView ->
                        match childView.TryGetTarget () with
                        | (true, childView) -> 
                            (childView :> obj :?> IView).InitializedEvent.Trigger ()
                        | _ -> ()
                    )
                )
                |> context.AddDisposable

                (view :> obj :?> IView).AppearingEvent.Publish
                |> Observable.subscribe (fun () ->
                    childrenViews
                    |> Seq.iter (fun childView ->
                        match childView.TryGetTarget () with
                        | (true, childView) -> 
                            (childView :> obj :?> IView).AppearingEvent.Trigger ()
                        | _ -> ()
                    )
                )
                |> context.AddDisposable
            )
        )

    module Dynamic =

        let inline text< ^T when ^T : (member set_Text : string -> unit) and
                            ^T :> View> (va: Val<string>) =
            OnceContext (fun context view ->
                context.Sink view (fun view value -> (^T : (member set_Text : string -> unit) (view, value))) va
            )

        let inline itemsSource< ^T when ^T :> FsListView> (va: Val<FormsDSL.Cell list>) =
            OnceContext (fun context view ->
                va |> context.Sink view (fun (view: ^T) values ->
                    view.ItemsSource <- values
                )
            )

    module Event =
       
        let inline textChanged< ^T when ^T :> View
                        and ^T : (member add_TextChanged : EventHandler<TextChangedEventArgs> -> unit)
                        and ^T : (member remove_TextChanged : EventHandler<TextChangedEventArgs> -> unit)> f =
            Subscribe (fun context view' ->
                let del = EventHandler<TextChangedEventArgs> (fun _ args -> f args.NewTextValue)
                (^T : (member add_TextChanged : EventHandler<TextChangedEventArgs> -> unit) (view', del))
                { new IDisposable with

                    member this.Dispose () =
                        (^T : (member remove_TextChanged : EventHandler<TextChangedEventArgs> -> unit) (view', del))
                }
                |> context.AddDisposable
            )

        let inline clicked< ^T when ^T :> View
                        and ^T : (member add_Clicked : EventHandler -> unit)
                        and ^T : (member remove_Clicked : EventHandler -> unit)> f =
            Subscribe (fun context view' ->
                let del = EventHandler (fun _ _ -> f ())
                (^T : (member add_Clicked : EventHandler -> unit) (view', del))
                { new IDisposable with

                    member this.Dispose () =
                        (^T : (member remove_Clicked : EventHandler -> unit) (view', del))
                }
                |> context.AddDisposable
            )

        let inline initialized< ^T when ^T :> View
                        and ^T :> IView> f =
            Subscribe (fun context (view: ^T) ->
                (view :> IView).InitializedEvent.Publish
                |> Observable.subscribe f
                |> context.AddDisposable
            )

        let inline appearing< ^T when ^T :> View
                        and ^T :> IView> f =
            Subscribe (fun context (view: ^T) ->
                (view :> IView).AppearingEvent.Publish
                |> Observable.subscribe f
                |> context.AddDisposable
            )

        let inline disappearing< ^T when ^T :> View
                        and ^T :> IView> f =
            Subscribe (fun context (view: ^T) ->
                (view :> IView).DisappearingEvent.Publish
                |> Observable.subscribe f
                |> context.AddDisposable
            )

    module Effect =

        let navigationPush (va: CmdVal<FormsDSL.Page>) =
            OnceContext (fun context view ->
                va |> context.SinkCmd context.Application (fun app page ->
                    page.Push app
                )
            )

[<AutoOpen>]
module ViewComponents =

    let stackLayout props children' =
        create 
            (fun _ -> FsStackLayout ()) 
            ([ children children' ] @ props)

    let absoluteLayout props children' =
        create 
            (fun _ -> FsAbsoluteLayout ()) 
            ([ children children' ] @ props)

    let label props =
        create (fun _ -> FsLabel ()) props

    let entry props onTextChanged =
         create 
            (fun _ -> FsEntry ()) 
            ([ Event.textChanged onTextChanged ] @ props)

    let button props onClick =
         create 
            (fun _ -> FsButton ())
            ([ Event.clicked onClick ] @ props)

    let image props =
        create (fun _ -> FsImage ()) props

    let listView props items =
        create (fun context -> 
            FsListView (WeakReference<Application> (context.Application))
        ) ([ Dynamic.itemsSource items ] @ props)

