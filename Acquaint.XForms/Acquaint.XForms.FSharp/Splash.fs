module Acquaint.XForms.Splash

open Sesame
open Xamarin.Forms
open FormsDSL

open Acquaint.XForms.Models

let createView () =
    let cmdNavigate = Cmd.create ()

    let navigate =
        cmdNavigate.Val
        |> CmdVal.map (AcquaintanceList.createPage)
        |> Effect.navigationPush 

    let appearing = 
        Event.appearing (fun () ->
            async {
                do! Async.Sleep 3000
                cmdNavigate.Execute ()
            } |> Async.Start
        )

    let imageSource = ImageSource.FromFile ("splash.png")

    absoluteLayout [ navigate; appearing ] [
        image [ 
            source imageSource; aspect Aspect.AspectFill; 
            absoluteLayoutBounds (Rectangle.FromLTRB (0., 0., 1., 1.)); absoluteLayoutFlags AbsoluteLayoutFlags.All 
        ]
    ]

