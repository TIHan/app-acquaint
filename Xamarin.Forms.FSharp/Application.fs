namespace Xamarin.Forms.FSharp

open System

open Sesame

open Xamarin.Forms

type Application =
    {
        Create: unit -> Xamarin.Forms.Application 
    }

type ApplicationAttribute< 'T when 'T :> Xamarin.Forms.Application> =
    | Once of ('T -> unit)
    | Dynamic of (Context -> 'T -> unit)

[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Application =

    let create (attribs: ApplicationAttribute<_> list) (app: Xamarin.Forms.Application) =
        {
            Create = fun () ->
                let context = new Context ()

                attribs
                |> List.iter (function
                    | Once f -> f app
                    | Dynamic f -> f context app
                )

                app
        }

    let build (application: Application) =
        application.Create ()

[<AutoOpen>]
module ApplicationAttributes =

    let inline mainPage< ^T when ^T : (member set_MainPage : Xamarin.Forms.Page -> unit) and
                            ^T :> Xamarin.Forms.Application> (value: BasePage) =
        Once (fun app -> (^T : (member set_MainPage : Xamarin.Forms.Page -> unit) (app, Page.build value)))

    [<RequireQualifiedAccess>]
    module Dynamic =

        let inline mainPage< ^T, ^U when ^T : (member set_MainPage : Xamarin.Forms.Page -> unit) and
                                ^T :> Xamarin.Forms.Application> (value: Val<BasePage>) =
            Dynamic (fun context app -> 
                context.Subscribe (fun value -> (^T : (member set_MainPage : Xamarin.Forms.Page -> unit) (app, Page.build value))) value
            )
