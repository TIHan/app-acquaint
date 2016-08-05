namespace FormsDSL

open System
open Sesame
open Xamarin.Forms

type FsCell (app: WeakReference<Application>, comp: ViewComponent) as this =
    inherit ViewCell ()

    let mutable context = Unchecked.defaultof<_>

    do
        context <- new FormsContext (app)
        this.View <- context.CreateView comp
        context.SubscribeView comp this.View
        (this.View :> obj :?> IView).InitializedEvent.Trigger ()

    override this.OnAppearing () =

        if context.PageInitialized then
            context.SubscribeView comp this.View

        context.PageInitialized <- true

        base.OnAppearing ()

        (this.View :> obj :?> IView).AppearingEvent.Trigger ()

    override this.OnDisappearing () =
        base.OnDisappearing ()

        (this.View :> obj :?> IView).DisappearingEvent.Trigger ()

        context.ClearDisposables ()

[<RequireQualifiedAccess>]
type Cell =
    | View of comp: ViewComponent * styleId: string * height: double

    member this.Build (app) =
        match this with
        | View (comp, styleId, height) ->
            let cell = FsCell (app, comp)
            cell.StyleId <- styleId
            cell.Height <- height
            cell :> Xamarin.Forms.Cell