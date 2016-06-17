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

module Models =

    type SplashViewModel = SplashViewModel of unit

    type AcquaintanceListViewModel = AcquaintanceListViewModel of unit

    type MainViewModel =
        | Splash of SplashViewModel
        | AcquaintanceList of AcquaintanceListViewModel

    type MainApplicationModel =
        {
            ViewModel: Var<MainViewModel>
        }

        static member Create () =
            {
                ViewModel = Var.create (SplashViewModel () |> Splash)
            }

        member this.GoToAcquaintanceList = async {
            let context = System.Threading.SynchronizationContext.Current
            do! Async.SwitchToThreadPool ()
            do! Async.Sleep (3000)
            do! Async.SwitchToContext context

            this.ViewModel.Set (AcquaintanceListViewModel () |> AcquaintanceList)
        }

module Pages =

    open Models
    open Views

    let splashPage (mainApplicationModel: MainApplicationModel) =
        contentPage splash
        |> onAppear mainApplicationModel.GoToAcquaintanceList

module Applications =

    open Pages
    open Models
    open Application

    let main (app: Xamarin.Forms.Application) nextPage =
        let model = MainApplicationModel.Create ()

        app
        |> create [
            model.ViewModel
            |> Val.ofVar
            |> Val.map (function
                | Splash vm -> 
                    (splashPage model).ToPage ()
                | AcquaintanceList vm ->
                    (Page.create (fun () -> nextPage) (fun _ _ -> ())).ToPage ()
            )
            |> Dynamic.mainPage
        ]
        |> build