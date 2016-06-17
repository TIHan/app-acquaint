namespace Xamarin.Forms.FSharp

open System

type Var<'T> =
    {
        mutable value: 'T
        callbacks: ResizeArray<'T -> unit>
    }

    member this.Value = this.value

    member this.Set value =
        this.value <- value
        this.Notify ()

    member this.Update f =
        this.value <- f this.value
        this.Notify ()

    member this.Notify () =
        for i = 0 to this.callbacks.Count - 1 do
            let f = this.callbacks.[i]
            f this.value

[<RequireQualifiedAccess>]
module Var =

    let create initialValue =
        {
            value = initialValue
            callbacks = ResizeArray ()
        }

type Val<'T> =
    {
        mutable value: 'T
        mutable isDirty: bool
        callbacks: ResizeArray<'T -> unit>
        subscriptions: ResizeArray<IDisposable>
    }

    member this.Value = this.value

    member this.Notify () =
        for i = 0 to this.callbacks.Count - 1 do
            let f = this.callbacks.[i]
            f this.value

    member this.Subscribe (callback: 'T -> unit) =
        this.callbacks.Add callback
        {
            new IDisposable with

                member __.Dispose () =
                    this.callbacks.Remove (callback) |> ignore
        }

[<RequireQualifiedAccess>]
module Val =

    let constant value =
        {
            value = value
            isDirty = true
            callbacks = ResizeArray ()
            subscriptions = ResizeArray ()
        }

    let ofVar (var: Var<'T>) : Val<'T> =
        let va = 
            {
                value = var.Value
                isDirty = true
                callbacks = var.callbacks
                subscriptions = ResizeArray ()
            }

        var.callbacks.Add (fun value ->
            va.value <- value
            va.isDirty <- true
        )

        va

    let map f (va: Val<_>) =
        let newVa = constant Unchecked.defaultof<_>

        newVa.subscriptions.Add (
            va.Subscribe (fun value ->
                newVa.value <- f value
                newVa.isDirty <- true
                newVa.Notify ()
            )
        )

        newVa

    let map2 f (va1: Val<_>) (va2: Val<_>) =
        let newVa = constant Unchecked.defaultof<_>
        let mutable hasLastSubscription = false

        newVa.subscriptions.Add (
            va1.Subscribe (fun value ->
                if hasLastSubscription then 
                    newVa.value <- f value va2.value
                    newVa.isDirty <- true
                    newVa.Notify ()
            )
        )

        newVa.subscriptions.Add (
            va2.Subscribe (fun value ->
                newVa.value <- f va1.value value
                newVa.isDirty <- true
                newVa.Notify ()
                hasLastSubscription <- true
            )
        )

        newVa

[<Sealed>]
type Context () =

    let disposables = ResizeArray<IDisposable> ()
    let mutable isDisposed = false

    member val WillForceImmediateUpdate = false with get, set

    member this.Subscribe f (va: Val<'T>) =
        if isDisposed then
            failwith "Context is disposed"

        if va.isDirty || this.WillForceImmediateUpdate then
            f va.value

        {
            new IDisposable with

                member this.Dispose () =
                    va.isDirty <- false
        }
        |> disposables.Add

        va.Subscribe f
        |> disposables.Add

    member this.AddDisposable x =
        if isDisposed then
            failwith "Context is disposed"

        disposables.Add(x)

    interface IDisposable with

        member this.Dispose () =
            disposables
            |> Seq.iter (fun x -> x.Dispose ())
            disposables.Clear ()
            isDisposed <- true
