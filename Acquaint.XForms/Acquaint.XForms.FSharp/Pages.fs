module Acquaint.XForms.Pages

open Sesame
open Xamarin.Forms
open Xamarin.Forms.FSharp

open System.Threading.Tasks

open Acquaint.XForms.Models
open Acquaint.XForms.Views

let splashPage (mainApplicationModel: MainApplicationModel) =
    contentPage splash
    |> Page.handlers [
        onAppearing (fun () ->
            mainApplicationModel.GoToAcquaintanceList
            |> Async.StartImmediate
        )
    ]
