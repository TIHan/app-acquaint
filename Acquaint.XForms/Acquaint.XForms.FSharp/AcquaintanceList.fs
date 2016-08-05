module Acquaint.XForms.AcquaintanceList

open Sesame
open Xamarin.Forms
open FormsDSL

open Acquaint.XForms.Models

let createAcquaintanceDetailView () =
    absoluteLayout [ ] [
        stackLayout [ ] [ ]
    ]

let createView () =
    let vm = Models.AcquaintanceListViewModel.Create ()

    let init =
        Event.initialized (fun () ->
            vm.Load () |> Async.Start
        )

    vm.Acquaintances.Val
    |> Val.map List.ofSeq
    |> Val.mapList (fun x ->
        label [ text x.FirstName ]
    )
    |> Val.mapList (fun x ->
        Cell.View (x, "disclosure", 60.)
    )
    |> listView [ init ]

let createPage () =
    Page.Content (createView (), [ { Icon = "edit.png"; Text = "New"; Activated = id } ])
