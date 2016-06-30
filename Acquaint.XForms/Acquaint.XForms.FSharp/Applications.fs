module Acquaint.XForms.Applications

open Sesame
open Xamarin.Forms
open Xamarin.Forms.FSharp

open System.Threading.Tasks

open Acquaint.XForms.Pages
open Acquaint.XForms.Models

let main (app: Xamarin.Forms.Application) nextPage =
    let model = MainApplicationModel.Create ()

    //let tmp = Page.create (fun () -> nextPage) (fun _ _ -> ())
    let tmp = contentPage (Views.acquaintanceList (AcquaintanceListViewModel.Create ()))

    app
    |> Application.create [
        model.ViewModel
        |> Val.ofVar
        |> Val.map (function
            | Splash vm -> 
                splashPage model :> BasePage
            | AcquaintanceList vm ->
                navigationPage (tmp) :> BasePage
        )
        |> Dynamic.mainPage
    ]
    |> Application.build
