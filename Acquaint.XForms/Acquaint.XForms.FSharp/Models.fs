module Acquaint.XForms.Models

open Sesame
open Acquaint.Data

type Acquaintance =
    {
        Id: string
        FirstName: string
        LastName: string
        Company: string
        JobTitle: string
        Email: string
        Phone: string
        Street: string
        City: string
        PostalCode: string
        State: string
        PhotoUrl: string
    }

type AcquaintanceListViewModel = 
    {
        DataSource: AcquaintanceDataSource
        Acquaintances: Var<Acquaintance seq>
    }

    static member Create () =
        {
            DataSource = new AcquaintanceDataSource()
            Acquaintances = Var.create Seq.empty
        }


    member this.Load () =
        async {
            let! data = this.DataSource.GetItems (0, 1000) |> Async.AwaitTask
            let data =
                data
                |> Seq.map (fun (x: Acquaint.Data.Acquaintance) ->
                    {
                        Id = x.Id
                        FirstName = x.FirstName
                        LastName= x.LastName
                        Company = x.Company
                        JobTitle = x.JobTitle
                        Email = x.Email
                        Phone = x.Phone
                        Street = x.Street
                        City = x.City
                        PostalCode = x.PostalCode
                        State = x.State
                        PhotoUrl = x.PhotoUrl
                    }
                )
            this.Acquaintances.Set data
        }
