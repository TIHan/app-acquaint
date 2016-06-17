namespace Acquaint.XForms

open Xamarin.Forms
open Xamarin.Forms.FSharp

open System.Threading.Tasks

module Views =

    let splash =
        absoluteLayout [ ] [
            image [ 
                source (ImageSource.FromFile ("splash.png")); aspect Aspect.AspectFill; 
                absoluteLayoutBounds (Rectangle.FromLTRB (0., 0., 1., 1.)); absoluteLayoutFlags AbsoluteLayoutFlags.All 
            ]
        ]

module Pages =

    open Page

    let splash nextPage =
        content Views.splash
        |> onAppear (async {
            do! Async.Sleep (3000)

            Application.Current.MainPage <- (navigation nextPage) |> build
        })