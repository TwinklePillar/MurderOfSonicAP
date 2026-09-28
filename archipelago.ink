=== HintGame ===
    -> RandomMusic ->
    >>>>Hold(1)
    >>>>InitFinalPushMinigame(1)
    # stage:rightmiddle
    # face:left
    # expression:explaining
    Tails:: It appears the player of this slot is wanting some hints
    # expression:thinking
    Tails:: Looks like I'm gonna need to hack the server for each hint
    # expression:determined
    Tails:: You know what that means
    # expression:excited
    # move:tails to:middle from:rightmiddle
    Tails:: It's time to THINK!
    >>>>StartFinalPushMinigame(1, HintGame.RandomHintCharacter)
    -> DONE
    = RandomMusic
        {shuffle:
    		- >>>>PlayMusic(spagonia, 0.5)
    		- >>>>PlayMusic(mirage_saloon, 0.5)
    		- >>>>PlayMusic(saloon1, 0.5)
    		- >>>>PlayMusic(interrogationsaloon, 0.5)
    		- >>>>PlayMusic(diningcar1, 0.5)
    		- >>>>PlayMusic(diningcar2, 0.5)
    		- >>>>PlayMusic(diningcar3, 0.5)
    		- >>>>PlayMusic(library1, 0.5)
    		- >>>>PlayMusic(interrogationlibrary, 0.5)
    		- >>>>PlayMusic(casino, 0.5)
    		- >>>>PlayMusic(whiteboardheist, 0.5)
    		- >>>>PlayMusic(lounge1, 0.5)
    		- >>>>PlayMusic(interrogationlounge, 0.5)
    		- >>>>PlayMusic(conductorcar1, 0.5)
    		- >>>>PlayMusic(conductorcar2, 0.5)
    		- >>>>PlayMusic(interrogationfinal, 0.5)
    		- >>>>PlayMusic(conductorcarlockdown, 0.5)
    		- >>>>PlayMusic(lockdownchase, 0.5)
    		- >>>>PlayMusic(returntoconductorcar, 0.5)
    		- >>>>PlayMusic(finalboss, 0.5)
    		- >>>>PlayMusic(saferoom1, 0.5)
    		- >>>>PlayMusic(saferoom2, 0.5)
    		- >>>>PlayMusic(diningcloset1, 0.5)
    		- >>>>PlayMusic(interrogationcloset, 0.5)
    		- >>>>PlayMusic(messydiningcar1, 0.5)
    		- >>>>PlayMusic(diningcarlockdown, 0.5)
    		- >>>>PlayMusic(lockdowncaroptimistic, 0.5)
    		- >>>>PlayMusic(namingbarry, 0.5)
        }
        ->->
    = RandomHintCharacter
        >>>>PlayStorySFX(traindoorbreak1)
        >>>>StageShowImage(standalonecomics, finalpushdoor1)
        >>>>PlayStorySFX(traindoorbreakjustbreak)
        >>>>StageShowImage(standalonecomics, finalpushdoor2)
        >>>>StageHideImage(standalonecomics)
        >>>>StopMusic(0.5)
        {shuffle:
            - -> HintAmy ->
            - -> HintBlaze ->
        /*    - -> HintConductor ->
            - -> HintEspio ->
            - -> HintKnuckles ->
            - -> HintRouge ->
            - -> HintShadow ->
            - -> HintSonic ->
            - -> HintTails ->
            - -> HintVector ->
        */
        }
        -> RandomMusic ->
        >>>>StartFinalPushMinigame(0, HintGame.RandomHintCharacter)
        -> DONE
    = HintAmy
        # move:amy from:offstageleft to:middle
        # expression:happy
        >>>>Hold(0.25)
        Amy:: That's a cool hint you got there!
        # expression:delighted
        Amy:: Let's keep going, maybe we can find more!
        # face:right
        >>>>Hold(0.25)
        # move:amy from:middle to:offstageright
        >>>>Hold(0.5)
        ->->
    = HintBlaze
        # move:blaze from:offstageleft to:middle
        # expression:talking
        Blaze:: This hint appears to be fine
        # expression:shocked
        Blaze:: I hope the next one is better!
        # face:right
        # expression:upset
        >>>>Hold(0.25)
        # move:blaze from:middle to:offstageright
        >>>>Hold(0.5)
        ->->
    = HintConductor
        ->->
    = HintEspio
        ->->
    = HintKnuckles
        ->->
    = HintRouge
        ->->
    = HintShadow
        ->->
    = HintSonic
        ->->
    = HintTails
        ->->
    = HintVector
        ->->

=== SonicTrap ===
    -> DONE

=== LiteratureTrap ===
    -> DONE