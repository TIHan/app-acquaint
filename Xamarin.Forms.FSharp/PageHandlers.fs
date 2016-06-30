namespace Xamarin.Forms.FSharp

open System

open Sesame

open Xamarin.Forms

type PageHandler<'T when 'T :> Page> = 
    | Handler of ('T -> IDisposable)

[<AutoOpen>]
module PageHandlers =

    let inline onAppearing< ^T when ^T :> Page
                    and ^T : (member add_Appearing : EventHandler -> unit)
                    and ^T : (member remove_Appearing : EventHandler -> unit)> f =
        Handler (fun page' ->
            let del = EventHandler (fun _ _ -> f ())
            (^T : (member add_Appearing : EventHandler -> unit) (page', del))
            { new IDisposable with

                member this.Dispose () =
                    (^T : (member remove_Appearing : EventHandler -> unit) (page', del))
            }
        )
