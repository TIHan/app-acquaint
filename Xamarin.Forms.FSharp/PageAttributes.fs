namespace Xamarin.Forms.FSharp

open System

open Sesame

open Xamarin.Forms

type PageAttribute<'T when 'T :> Page> = 
    | Once of ('T -> unit)
    | Dynamic of ('T -> Context -> unit)

[<AutoOpen>]
module PageAttributes =

    module Dynamic =

        let inline navigationPush< ^T when ^T :> Page
                            and ^T : (member get_Navigation : unit -> INavigation)> value =
            Dynamic (fun page' context -> 
                let navigation = (^T : (member get_Navigation : unit -> INavigation) (page'))

                value
                |> context.Subscribe (fun value -> navigation.PushAsync (value) |> ignore)
            )

