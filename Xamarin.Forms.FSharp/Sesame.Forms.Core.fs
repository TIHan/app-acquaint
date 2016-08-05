namespace FormsDSL

open System

open Sesame

type XView = Xamarin.Forms.View

type CustomViewProperty =
    | Once of (FormsContext -> XView -> unit)
    | Subscribe of (FormsContext -> XView -> unit)
    | OnceAndSubscribe of (FormsContext -> XView -> unit) * (FormsContext -> XView -> unit)

and CustomView =
    {
        Create: FormsContext -> XView
        Subscribe: FormsContext -> XView -> unit
    }

and View =
    | Custom of CustomView

//and ViewComponent (create: FormsContext -> View, subscribe: FormsContext -> View -> unit) =

//    member this.CreateView = create

//    member this.SubscribeView = subscribe

//    static member Create<'T when 'T :> View> (create: FormsContext -> 'T, subscribe: FormsContext -> 'T -> unit) =
//        ViewComponent (
//            (fun context -> create context :> View), 
//            (fun context o -> subscribe context (o :?> 'T))
//        )

//and ViewComponentProperty<'T when 'T :> View> =
//    | Once of ('T -> unit)
//    | OnceContext of (FormsContext -> 'T -> unit)
//    | Subscribe of (FormsContext -> 'T -> unit)
//    | OnceAndSubscribe of (FormsContext -> 'T -> unit) * (FormsContext ->'T -> unit)

//and FormsContext (app: WeakReference<Application>) =
//    inherit Context (Device.BeginInvokeOnMainThread)

//    member this.CreateView (comp: ViewComponent) =
//        comp.CreateView this

//    member this.SubscribeView (comp: ViewComponent) =
//        comp.SubscribeView this

//    member this.Application =
//        match app.TryGetTarget () with
//        | true, app -> app
//        | _ -> null

//    member val PageInitialized = false with get, set

and FormsContext (app: WeakReference<Xamarin.Forms.Application>) =
    inherit Context (Xamarin.Forms.Device.BeginInvokeOnMainThread)

    member this.CreateView (view: View) =
        match view with
        | Custom customView -> customView.Create this

    member this.SubscribeView (view: View) =
        match view with
        | Custom customView -> customView.Subscribe this

    member this.Application =
        match app.TryGetTarget () with
        | true, app -> app
        | _ -> null

    member val PageInitialized = false with get, set

type IView =

    abstract InitializedEvent : Event<unit>

    abstract AppearingEvent : Event<unit>

    abstract DisappearingEvent : Event<unit>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module View =

    let lift f props =
        {
            Create =
                fun context ->
                    let xView : _ = f context

                    props
                    |> List.iter (function
                        | Once f -> f context xView
                        | OnceAndSubscribe (f, _) -> f context xView
                        | _ -> ()
                    )

                    xView

            Subscribe =
                fun context xView ->
                    props
                    |> List.iter (function
                        | Subscribe f -> f context xView
                        | OnceAndSubscribe (_, f) -> f context xView
                        | _ -> ()
                    ) 
        }
        |> View.Custom

//[<AutoOpen>]
//module ViewComponentModule =

//    let create f (props: ViewComponentProperty<'T> list) =
//        ViewComponent.Create<'T> (
//            (fun context ->
//                let view : 'T = f context

//                props
//                |> List.iter (function
//                    | Once f -> f view
//                    | OnceContext f -> f context view
//                    | OnceAndSubscribe (f, _) -> f context view
//                    | _ -> ()
//                )

//                view
//            ), 
//            (fun context view ->
//                props
//                |> List.iter (function
//                    | Subscribe f -> f context view
//                    | OnceAndSubscribe (_, f) -> f context view
//                    | _ -> ()
//                )
//            )
//       )