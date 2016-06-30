namespace Xamarin.Forms.FSharp

open System

open Sesame

open Xamarin.Forms

[<AbstractClass>]
type BasePage () =

    abstract Create : unit -> Page

    abstract Subscribe : Page -> Context -> unit

type Page<'T when 'T :> Page> (create: unit -> 'T, subscribe: 'T -> Context -> unit) =
    inherit BasePage ()

    override this.Create () = create () :> Page

    override this.Subscribe page context = subscribe (page :?> 'T) context

    member this.Extend (f, subscribe2) =
        Page<'T> (
            (fun () ->
                let page = create ()
                f page
                page
            ),
            (fun page context ->
                subscribe page context
                subscribe2 page context 
            )
        )

[<RequireQualifiedAccess>]
module Page =

    let create f subscribe =
        Page<_> (f, subscribe)

    let inline extend f subscribe (page: Page<_>) =
        page.Extend (f, subscribe)

    let build (page: BasePage) =
        let page' = page.Create ()
        match (page' :> obj) with
        | :? FSharpContentPage as page'' ->
            page''.Subscribe <- page.Subscribe page'
        | _ -> ()
        page'

    let attributes (attribs: PageAttribute<_> list) (page: Page<_>) =
        page
        |> extend
            (fun page' ->
                attribs
                |> List.iter (function
                    | Once f -> f page'
                    | _ -> ()
                )
            )

            (fun page' context ->
                attribs
                |> List.iter (function
                    | Dynamic f -> f page' context
                    | _ -> ()
                )
            )

    let handlers (handlers: PageHandler<_> list) (page: Page<_>) =
        page
        |> extend
            (fun _ -> ())

            (fun page' context ->
                handlers
                |> List.iter (function
                    | Handler f -> f page' |> context.AddDisposable
                )
            )

[<AutoOpen>]
module Pages =

    let contentPage view =
        Page.create
            (fun () -> FSharpContentPage (view))
            (fun _ _ -> ())

    let navigationPage (page: Page<_>) =
        Page.create
            (fun () -> NavigationPage (page.Create ()))
            (fun _ _ -> ())
