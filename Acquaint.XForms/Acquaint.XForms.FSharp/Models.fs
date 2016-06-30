module Acquaint.XForms.Models

open Sesame

type SplashViewModel = SplashViewModel of unit

type AcquaintanceListViewModel = 
    {
        Acquaintances: Var<string ResizeArray>
    }

    static member Create () =
        {
            Acquaintances = 
                List.init 10 (fun i -> "hopac" + string i)
                |> List.append (List.init 10 (fun i -> "jopac" + string i))
                |> List.append (List.init 20 (fun i -> "hopac1234" + string i))
                |> ResizeArray |> Var.create
        }

type MainViewModel =
    | Splash of SplashViewModel
    | AcquaintanceList of AcquaintanceListViewModel

    static member CreateSplash () =
        SplashViewModel ()
        |> Splash

    static member CreateAcquaintanceList () =
        AcquaintanceListViewModel.Create ()
        |> AcquaintanceList

type MainApplicationModel =
    {
        ViewModel: Var<MainViewModel>
    }

    static member Create () =
        {
            ViewModel = 
                MainViewModel.CreateSplash ()
                |> Var.create
        }

    member this.GoToAcquaintanceList = async {
        let context = System.Threading.SynchronizationContext.Current
        do! Async.SwitchToThreadPool ()
        do! Async.Sleep (3000)
        do! Async.SwitchToContext context

        MainViewModel.CreateAcquaintanceList ()
        |> this.ViewModel.Set
    }
