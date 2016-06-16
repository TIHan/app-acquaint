namespace Xamarin.Forms.FSharp.iOS

open System

open Xamarin.Forms

type Var<'T> =
    {
        mutable value: 'T
        callbacks: ResizeArray<unit -> unit>
    }

    member this.Value = this.value

    member this.SetValue value =
        this.value <- value
        this.Notify ()

    member this.Update f =
        this.value <- f this.value
        this.Notify ()

    member this.Notify () =
        this.callbacks.ForEach (fun f -> f ())

    member this.Subscribe (callback: unit -> unit) =
        callback ()
        this.callbacks.Add callback
        {
            new IDisposable with

                member __.Dispose () =
                    this.callbacks.Remove (callback) |> ignore
        }

type Val<'T> =
    {
        mutable value: 'T
        callbacks: ResizeArray<unit -> unit>
        subscriptions: ResizeArray<IDisposable>
    }

    member this.Value = this.value

    member this.Notify () =
        this.callbacks.ForEach (fun f -> f ())

    member this.Subscribe (callback: unit -> unit) =
        callback ()
        this.callbacks.Add callback
        {
            new IDisposable with

                member __.Dispose () =
                    this.callbacks.Remove (callback) |> ignore
        }

[<Sealed>]
type Var =

    static member Create (initialValue) : Var<_> =
        {
            value = initialValue
            callbacks = ResizeArray ()
        }

[<Sealed>]
type Val =

    static member Create (initialValue) : Val<_> =
        {
            value = initialValue
            callbacks = ResizeArray ()
            subscriptions = ResizeArray ()
        }

    static member FromVar (var: Var<'a>) =
        let va = Val.Create (Unchecked.defaultof<'a>)
        va.subscriptions.Add (var.Subscribe (fun () -> 
                va.value <- var.value
                va.Notify ()
            )
        )
        va

    static member Map f (var: Val<_>) =
        let newVar = Val.Create (Unchecked.defaultof<_>)
        newVar.subscriptions.Add (var.Subscribe (fun () -> 
                newVar.value <- f var.value
                newVar.Notify ()
            )
        )
        newVar

    static member Map2 f (var1: Val<_>) (var2: Val<_>) =
        let newVar = Val.Create (Unchecked.defaultof<_>)
        let mutable hasLastSubscription = false
        newVar.subscriptions.Add (var1.Subscribe (fun () ->
                if hasLastSubscription then 
                    newVar.value <- f var1.value var2.value
                    newVar.Notify ()
            )
        )
        newVar.subscriptions.Add (var2.Subscribe (fun () ->
                newVar.value <- f var1.value var2.value
                newVar.Notify ()
                hasLastSubscription <- true
            )
        )
        newVar

type FormsContext =
    {
        Disposables: IDisposable ResizeArray
    }

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

    let children (children: BaseView list) (view: View<StackLayout>) =
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

    module Dynamic =

        let children (children: Val<BaseView list>) (view: View<StackLayout>) =
            let childrenViews = ResizeArray<WeakReference<View>> ()

            view
            |> extend 
                (fun context view ->
                    ()
                )

                (fun context view' ->

                    children.Subscribe (fun () ->
                        childrenViews
                        |> Seq.iter (fun childView ->
                            match childView.TryGetTarget () with
                            | (true, childView) -> view'.Children.Remove (childView) |> ignore
                            | _ -> ()
                        )

                        children.Value
                        |> Seq.iter (fun child ->
                            let childView = child.CreateView context
                            childrenViews.Add(WeakReference<View> (childView))
                            view'.Children.Add (childView)
                        )
                    )
                    |> ignore
                )


module Views =

    let stackLayout =
        View<StackLayout> (
            (fun _ ->
                let stackLayout = StackLayout ()

                stackLayout
            ),

            (fun context stackLayout ->
                ()
            )
        )
