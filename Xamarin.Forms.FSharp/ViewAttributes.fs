namespace Xamarin.Forms.FSharp

open System

open Xamarin.Forms

type ViewAttribute<'T when 'T :> View> = 
    | Once of ('T -> unit)
    | Dynamic of (Context -> 'T -> unit)

[<AutoOpen>]
module ViewAttributes =

    let inline verticalOptions< ^T when ^T : (member set_VerticalOptions : LayoutOptions -> unit) and
                            ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_VerticalOptions : LayoutOptions -> unit) (el, value)))

    let inline horizontalOptions< ^T when ^T : (member set_HorizontalOptions : LayoutOptions -> unit) and
                            ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_HorizontalOptions : LayoutOptions -> unit) (el, value)))

    let inline text< ^T when ^T : (member set_Text : string -> unit) and
                        ^T :> View> value =
        Once (fun (el: ^T) -> (^T : (member set_Text : string -> unit) (el, value)))

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

    let inline absoluteLayoutFlags< ^T when ^T :> View> value =
        Once (fun (el: ^T) -> AbsoluteLayout.SetLayoutFlags (el, value))   

    let inline absoluteLayoutBounds< ^T when ^T :> View> value =
        Once (fun (el: ^T) -> AbsoluteLayout.SetLayoutBounds (el, value))

    module Dynamic =

        let inline text< ^T when ^T : (member set_Text : string -> unit) and
                            ^T :> View> (va: Val<string>) =
            Dynamic (fun context (el: ^T) ->
                context.Subscribe (fun value -> (^T : (member set_Text : string -> unit) (el, value))) va
            )

    //let inline textBinding< ^T when ^T : (member set_Text : string -> unit) and
    //                    ^T : (member get_Text : unit -> string) and
    //                    ^T : (member add_TextChanged : System.EventHandler<TextChangedEventArgs> -> unit) and
    //                    ^T :> View> (var: Var<string>) =
    //    Dynamic (fun context (el: ^T) ->
    //        ()
    //        //let handler = (^T : (member add_TextChanged : System.EventHandler<TextChangedEventArgs> -> unit) (el))

    //        //var.Subscribe (fun () -> (^T : (member set_Text : string -> unit) (el, var.Value)))
    //    )
