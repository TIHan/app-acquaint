namespace FormsDSL

open System
open Sesame
open Xamarin.Forms

type FsContentPage (app: WeakReference<Application>, comp: ViewComponent) as this =
    inherit ContentPage ()

    let mutable context = Unchecked.defaultof<_>

    do
        context <- new FormsContext (app)
        this.Content <- context.CreateView comp
        context.SubscribeView comp this.Content
        (this.Content :> obj :?> IView).InitializedEvent.Trigger ()

    override this.OnAppearing () =
        GC.Collect ()

        if context.PageInitialized then
            context.SubscribeView comp this.Content

        context.PageInitialized <- true

        base.OnAppearing ()

        (this.Content :> obj :?> IView).AppearingEvent.Trigger ()

    override this.OnDisappearing () =
        base.OnDisappearing ()

        (this.Content :> obj :?> IView).DisappearingEvent.Trigger ()

        context.ClearDisposables ()

type ToolbarItem =
    {
        Text: string
        Icon: string
        Activated: unit -> unit
    }

[<RequireQualifiedAccess>]
type Page =
    | Content of content: ViewComponent * toolbarItems: ToolbarItem list

    member this.Push (app: Application) =
        match this with
        | Content (comp, toolbarItems) ->
            let contentPage = FsContentPage (WeakReference<Application> (app), comp)

            toolbarItems
            |> List.iter (fun x ->
                Xamarin.Forms.ToolbarItem (x.Text, x.Icon, Action (x.Activated))
                |> contentPage.ToolbarItems.Add
            )

            try
                app.MainPage.Navigation.PushAsync (contentPage) |> ignore
            with | _ ->
                app.MainPage <- contentPage |> NavigationPage
