namespace Xamarin.Forms.FSharp

open System

open Sesame

open Xamarin.Forms

type ViewHandler<'T when 'T :> View> = 
    | Handler of ('T -> IDisposable)

[<AutoOpen>]
module ViewHandlers =

    let inline textChanged< ^T when ^T :> View
                    and ^T : (member add_TextChanged : EventHandler<TextChangedEventArgs> -> unit)
                    and ^T : (member remove_TextChanged : EventHandler<TextChangedEventArgs> -> unit)> f =
        Handler (fun view' -> 
            let del = EventHandler<TextChangedEventArgs> (fun _ args -> f args.NewTextValue)
            (^T : (member add_TextChanged : EventHandler<TextChangedEventArgs> -> unit) (view', del))
            { new IDisposable with

                member this.Dispose () =
                    (^T : (member remove_TextChanged : EventHandler<TextChangedEventArgs> -> unit) (view', del))
            }
        )

    let inline clicked< ^T when ^T :> View
                    and ^T : (member add_Clicked : EventHandler -> unit)
                    and ^T : (member remove_Clicked : EventHandler -> unit)> f =
        Handler (fun view' -> 
            let del = EventHandler (fun _ _ -> f ())
            (^T : (member add_Clicked : EventHandler -> unit) (view', del))
            { new IDisposable with

                member this.Dispose () =
                    (^T : (member remove_Clicked : EventHandler -> unit) (view', del))
            }
        )
