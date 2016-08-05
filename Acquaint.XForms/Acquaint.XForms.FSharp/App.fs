namespace Acquaint.XForms

open Xamarin.Forms
open FormsDSL
open System

type App () as this =
    inherit Application ()

    do
        if Device.OS = TargetPlatform.Android then
            this.MainPage <- 
                FsContentPage (WeakReference<Application> (this), Splash.createView ())
        else
            (AcquaintanceList.createPage ()).Build (this)
       
