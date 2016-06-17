namespace Xamarin.Forms.FSharp

open System

[<Sealed>]
type Var<'T> =

    member Value : 'T

    member Set : 'T -> unit

    member Update : ('T -> 'T) -> unit

[<RequireQualifiedAccess>]
module Var =

    val create : initialValue: 'T -> Var<'T>

[<Sealed>]
type Val<'T> =

    member Value : 'T

[<RequireQualifiedAccess>]
module Val =

    val constant : 'T -> Val<'T>

    val ofVar : Var<'T> -> Val<'T>

    val map : ('T -> 'U) -> Val<'T> -> Val<'U>

    val map2 : ('T1 -> 'T2 -> 'U) -> Val<'T1> -> Val<'T2> -> Val<'U>

[<Sealed>]
type Context =

    new : unit -> Context

    member WillForceImmediateUpdate : bool with get, set

    member Subscribe : ('T -> unit) -> Val<'T> -> unit

    member AddDisposable : IDisposable -> unit

    interface IDisposable


