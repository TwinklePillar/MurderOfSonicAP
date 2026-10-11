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
    >>>>StartFinalPushMinigame(1, HintGame.ToNextHintgame)
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
        >>>>ScreenShake(90, 90)
        >>>>StageShowImage(standalonecomics, finalpushdoor2)
        >>>>StageHideImage(standalonecomics)
        >>>>StopMusic(0.5)
    /*
        {shuffle:
            - -> HintAmy ->
            - -> HintBlaze ->
            - -> HintConductor ->
            - -> HintEspio ->
            - -> HintKnuckles ->
            - -> HintRouge ->
            - -> HintShadow ->
            - -> HintSonic ->
            - -> HintTails ->
            - -> HintVector ->
        }
    */
        -> RandomMusic ->
        ->->
    = ToNextHintgame
        >>>>Hold(1)
        {cycle:
            -
            -
            -
            -
            - -> RandomHintCharacter ->
        }
        >>>>StartFinalPushMinigame(0, HintGame.ToNextHintgame)
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
        # move:conductor from:offstageleft to:middle
        # expression:welcoming
        Conductor:: What a fine hint!
        # expression:laughing
        Conductor:: I hope the next one is better!
        # face:right
        >>>>Hold(0.25)
        # move:conductor from:middle to:offstageright
        >>>>Hold(0.5)
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

=== ReturnToNoDialog ===
	>>>EndConversation()
	-> DONE

=== DeadTrap ===
    Barry:: So how's the sync going?
    # exit:all
    # enter:sonic
    # stage:middle
    # expression:winking
    Sonic:: I'm feeling fine, don't worry about me
    # expression:determined
    Sonic:: It's just a small poke with a blowdart
    # expression:panicking
    >>>>Hold(1)
    # expression:embarrassed
    >>>>Hold(1)
    # expression:injured
    >>>>Hold(1)
    # exit:sonic
    >>>>Hold(1)
    >>>>StageShowImage(DeadSonic, deadsonic)
    Barry:: SONIC!!!
    >>>>StageHideImage(DeadSonic)
    Barry:: I guess that means we need to solve the case of
    >>>>FadeOutDialogAndHUD(1)
    >>>>Hold(1)
    >>>>StageShowImage(inlinecomics, titlecard, 1)
    >>>>Hold(2)
    >>>>StageHideImage(inlinecomics)
    >>>>ReturnFromTrap()
    -> DONE

=== FakerTrap ===
    Barry:: So how's the sync going?
    # exit:all
    # enter:sonic
    # move:sonic from:offstageleft to:left
    # expression:happy
    # face:right
    Sonic:: Phew, so far so good!
    # expression:mad
    Sonic:: Hey, that's...
    # enter:shadow
    # move:shadow from:offstageright to:right
    # expression:grumpy
    Shadow:: That blue hedgehog again, of all places...
    Sonic:: I found you, FAKER!
    # expression:talking
    Shadow:: Faker? I think YOU'RE the fake hedgehog around here.
    # expression:embarrassed
    Shadow:: You're comparing yourself to me?
    # expression:tsundere
    Shadow:: Hah, you aren't even good enough to be my fake
    # expression:upset
    Sonic:: I'll make you eat those words!
    # move:sonic from:left to:middle
    # move:shadow from:right to:middle
    >>>>FadeToWhite(0.25)
    # exit:allinstant
    >>>>Hold(1)
    >>>>FadeFromWhite(1)
    >>>>ReturnFromTrap()
    -> DONE

=== ThinkTrap ===
    Barry:: So how's the sync going?
    Barry:: I'm starting to zone out, losing my train of thought
    Barry:: What's being said here again?
    -> StartThink
    = StartThink
        ~ temp rng = RANDOM(18, 30)
        {rng == 19: -> StartThink}
        {rng == 24: -> StartThink}
        {rng == 28: -> StartThink}
        >>>>StartMinigame({rng},ThinkTrap.PostThink)
        -> PostThink("False")
    = PostThink(playerWon)
        -> HintGame.RandomMusic ->
        {playerWon != "True":
            Barry:: Let me try that again
            -> StartThink
        }
        Barry:: Yep, back on track now!
        Barry:: Wait, the conversation is over?
        >>>>ReturnFromTrap()
        -> DONE