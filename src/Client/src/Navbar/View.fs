module Navbar.View

open Fable.Helpers.React
open Fable.Helpers.React.Props

let navButton classy href faClass txt =
    p
        [ ClassName "control" ]
        [ a
            [ ClassName (sprintf "button %s" classy);
              Href href;
              Target "_blank" ]
            [ span
                [ ClassName "icon" ]
                [ i
                    [ ClassName (sprintf "fa %s" faClass) ]
                    [ ] ]
              span
                [ ]
                [ str txt ] ] ]

let navButtons =
    span
        [ ClassName "nav-item" ]
        [ div
            [ ClassName "field is-grouped" ]
            [ navButton "linkedin" "https://www.linkedin.com/in/william-tetlow-1086ab9a/" "fa-linkedin" "LinkedIn"
              navButton "github" "https://github.com/williamtetlow" "fa-github" "Github"] ]

let root =
    nav
        [ ClassName "navbar is-white" ]
        [ div [ ClassName "container" ]
              [ div [ ClassName "navbar-brand" ]
                    [ a [ ClassName "nav-item padding-top-sm"; Href "/" ]
                        [ div [ ClassName "columns" ]
                              [ div [ ClassName "column is-2" ]
                                    [ figure  [ ClassName "image is-32x32"] 
                                              [ img [ Src "/img/stroopwafel.svg" ]  ]
                                    ]
                                div [ ClassName "column" ]
                                    [ div [ ClassName "title"]
                                          [ str "William Tetlow" ]
                                    ]
                              ] 
                        ]  
                  ]
                div [ ClassName "navbar-menu" ]
                    [ div [ ClassName "navbar-start" ]
                          [ ]
                      div [ ClassName "navbar-end" ]
                          [ div [ ClassName "navbar-item" ]  
                                [ navButtons ]  ]                          
                    ]                            
               ]
        ]             