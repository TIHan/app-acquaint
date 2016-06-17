namespace Xamarin.Forms.FSharp

open System

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
            this.Content <- view.CreateView ()
            isInitialized <- true
            context.WillForceImmediateUpdate <- true

        view.SubscribeView context this.Content
        this.Appeared.Trigger ()

    override this.OnDisappearing () =
        base.OnDisappearing ()

        this.Disappeared.Trigger ()

        (context :> IDisposable).Dispose ()
        context <- Unchecked.defaultof<_>

type Page<'T when 'T :> Page> =
    {
        Create: unit -> 'T
        Subscribe: 'T -> Context -> unit
    }

    member this.ToPage () : Page<Page> =
        {
            Create = fun () ->
                this.Create () :> Page

            Subscribe = fun page context ->
                this.Subscribe (page :?> 'T) context
        }

[<RequireQualifiedAccess>]
module Page =

    let create f subscribe =
        {
            Create = f
            Subscribe = subscribe
        }

    let build (page: Page<'T>) =
        let page' = page.Create ()
        match (page' :> obj) with
        | :? FSharpContentPage as page'' ->
            page''.Subscribe <- page.Subscribe page'
        | _ -> ()
        page'

[<AutoOpen>]
module PageAttributes =

    let onAppear (f: Async<unit>) (page: Page<FSharpContentPage>) =
        {
            Create = fun () ->
                let page' = page.Create ()

                page'

            Subscribe = fun page' context ->
                page'.Appeared.Publish.Subscribe (fun () ->
                    Async.StartImmediate f
                )
                |> context.AddDisposable
        }

    let navigationPush (value: Val<Page<_> option>) (page: Page<FSharpContentPage>) =
        {
            Create = fun () ->
                let page' = page.Create ()

                page'

            Subscribe = fun page' context ->
                page.Subscribe page' context

                value
                |> context.Subscribe (function
                    | Some page ->
                        page |> Page.build
                        |> page'.Navigation.PushAsync
                        |> ignore
                    | _ -> ()
                )
        }

[<AutoOpen>]
module Pages =

    let contentPage view =
        {
            Create = fun () -> FSharpContentPage (view)
            Subscribe = fun _ _ -> ()
        }

    let navigationPage page =
        {
            Create = fun () -> NavigationPage (page.Create ())
            Subscribe = fun _ _ -> ()
        }

type Application =
    {
        Create: unit -> Xamarin.Forms.Application 
    }

type ApplicationAttribute< 'T when 'T :> Xamarin.Forms.Application> =
    | Once of ('T -> unit)
    | Dynamic of (Context -> 'T -> unit)

[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module Application =

    let create (attribs: ApplicationAttribute<_> list) (app: Xamarin.Forms.Application) =
        {
            Create = fun () ->
                let context = new Context ()

                attribs
                |> List.iter (function
                    | Once f -> f app
                    | Dynamic f -> f context app
                )

                app
        }

    let build (application: Application) =
        application.Create ()

    let inline mainPage< ^T when ^T : (member set_MainPage : Xamarin.Forms.Page -> unit) and
                            ^T :> Xamarin.Forms.Application> (value: Page<_>) =
        Once (fun app -> (^T : (member set_MainPage : Xamarin.Forms.Page -> unit) (app, Page.build value)))

    [<RequireQualifiedAccess>]
    module Dynamic =

        let inline mainPage< ^T when ^T : (member set_MainPage : Xamarin.Forms.Page -> unit) and
                                ^T :> Xamarin.Forms.Application> (value: Val<Page<_>>) =
            Dynamic (fun context app -> 
                context.Subscribe (fun value -> (^T : (member set_MainPage : Xamarin.Forms.Page -> unit) (app, Page.build value))) value
            )
