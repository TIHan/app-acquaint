module Acquaint.XForms.Views

open Sesame
open Xamarin.Forms
open Xamarin.Forms.FSharp

open Acquaint.XForms.Models

let splash =
    absoluteLayout [ ] [
        image [ 
            source (ImageSource.FromFile ("splash.png")); aspect Aspect.AspectFill; 
            absoluteLayoutBounds (Rectangle.FromLTRB (0., 0., 1., 1.)); absoluteLayoutFlags AbsoluteLayoutFlags.All 
        ]
    ]

let acquaintanceList (vm: AcquaintanceListViewModel) =
    let acquaintances = 
        vm.Acquaintances 
        |> Val.ofVar
        |> Val.map (fun items ->
            items
            |> Seq.map (fun str ->
                if (str.Contains("hopac")) then
                    stackLayout [ horizontalOptions LayoutOptions.Center; widthRequest 1000. ] [
                        label [ text str; horizontalOptions LayoutOptions.Center ]
                    ] :> BaseView
                else
                    stackLayout [ horizontalOptions LayoutOptions.Center ] [
                        label [ text str; horizontalOptions LayoutOptions.Center ]
                    ] :> BaseView
            )
            |> ResizeArray
        )

    absoluteLayout [ ] [
        listView [ ] acquaintances
    ]
