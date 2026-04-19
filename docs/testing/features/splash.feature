Feature: Splash screen
  The title screen is the user's entry point. It routes into:
  - New Game → class select → town (when slots are available)
  - New Game → slots-full dialog → Open Load Game | Cancel (when all 3 slots occupied)
  - Continue → Load Game screen (when at least one save exists)
  - Tutorial / Settings → info panels
  - Exit Game → quit
  Focus state + keyboard nav must survive panel-open/close and
  Continue→Back round-trips.

  Background:
    Given the game is launched
    And the splash screen is visible
    And a splash button has keyboard focus

  # Test: scripts/testing/tests/SplashTests.cs :: Splash_HasNewGameButton
  Scenario: New Game button exists and is focusable
    Then a button labeled "New Game" is visible on the splash screen
    And the "New Game" button can receive keyboard focus

  # Test: Splash_EnterOnNewGameOpensClassSelect
  Scenario: New Game click opens class select (clean save state)
    Given no save slots are occupied
    When the user focuses "New Game" and presses Enter
    Then the class-select screen appears

  # Test: Splash_NewGameWithAllSlotsFull_ShowsSlotsFullDialog
  Scenario: New Game click with all 3 save slots full opens the slots-full dialog
    Given all 3 save slots are occupied by warriors
    When the user focuses "New Game" and presses Enter
    Then a modal titled "ALL SAVE SLOTS ARE FULL" appears
    And the modal has an "Open Load Game" button
    And the modal has a "Cancel" button
    When the user clicks "Open Load Game"
    Then the Load Game screen appears

  # Test: Splash_NewGameWithAllSlotsFull_CancelReturnsToSplash
  Scenario: Slots-full dialog Cancel keeps user on splash
    Given all 3 save slots are occupied by warriors
    When the user clicks "New Game"
    And the slots-full dialog appears
    And the user clicks "Cancel"
    Then the slots-full dialog closes
    And the Load Game screen is NOT opened
    And the splash screen is still visible

  # Test: Splash_ContinueDisabledWhenNoSaves
  Scenario: Continue button is disabled when no saves exist
    Given no save slots are occupied
    Then the "Continue" button is disabled

  # Test: Splash_AfterContinueBack_NewGameStillWorks
  Scenario: New Game still works after Continue -> Back round-trip (focus-preservation bug)
    Given a save in slot 0
    When the user clicks "Continue"
    And the Load Game screen appears
    And the user presses Escape
    And the splash screen is re-shown
    And the user clicks "New Game"
    Then the class-select screen appears

  # Test: Splash_AfterDeleteSlotBack_NewGameStillWorks
  Scenario: New Game still works after deleting a save slot (lifecycle bug)
    Given a save in slot 0
    When the user clicks "Continue"
    And the Load Game screen appears
    And the user deletes slot 0
    And the Load Game screen rebuilds itself
    And the user presses Escape
    And the splash screen is re-shown
    And the user clicks "New Game"
    Then the class-select screen appears

  # Test: Splash_ClickingTutorial_OpensTutorialPanel
  Scenario: Tutorial button opens tutorial panel; Escape closes it
    When the user clicks "Tutorial"
    Then the TutorialPanel appears
    When the user presses Escape
    Then the TutorialPanel closes

  # Test: Splash_ClickingSettings_OpensSettingsPanel
  Scenario: Settings button opens settings panel; Escape closes it
    When the user clicks "Settings"
    Then the SettingsPanel appears
    When the user presses Escape
    Then the SettingsPanel closes
