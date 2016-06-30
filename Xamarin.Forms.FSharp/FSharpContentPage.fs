namespace Xamarin.Forms.FSharp

open System

open Sesame

open Xamarin.Forms

type FSharpContentPage (view: BaseView) =
    inherit ContentPage ()

    let mutable isInitialized = false
    let mutable context = Unchecked.defaultof<_>

    member val Subscribe : Context -> unit = fun _ -> () with get, set

    member val Appeared = Event<unit> () with get

    member val Disappeared = Event<unit> () with get

    override this.OnAppearing () =
        base.OnAppearing ()

        context <- new Context ()

        this.Subscribe context

        if not isInitialized then
            this.Content <- view.Create ()
            isInitialized <- true
            context.WillForceImmediateUpdate <- true

        view.Subscribe this.Content context
        this.Appeared.Trigger ()

    override this.OnDisappearing () =
        base.OnDisappearing ()

        this.Disappeared.Trigger ()

        (context :> IDisposable).Dispose ()
        context <- Unchecked.defaultof<_>
