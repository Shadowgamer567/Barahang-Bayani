# Barahang Bayani — Game Feature Guide

This guide describes the features represented in the Unity project: the player-facing game, classroom and account tools, quiz authoring and synchronization, saved progress, and supporting presentation systems. It is based on the project scripts, scenes, and data assets. Features that depend on Firebase or scene setup require those services and references to be configured in the running build.

## 1. Game at a glance

Barahang Bayani is a campaign-based educational card battler. The player progresses through a map and levels, watches story scenes, and fights enemy groups with a team of Filipino historical characters. Quiz cards present curriculum questions during combat; the project records quiz performance alongside campaign progress.

The main configured scenes are `MainMenu`, `MapMenu`, `LevelSelect_Tutorial`, `LevelSelect_Rizal`, and `Level`. `BackgroundTemplates` is a presentation/template scene. Unity recovery scenes under `Assets/_Recovery` are editor recovery artifacts, not normal destinations in the player flow.

## 2. Player walkthrough

### Choose an account

The main menu requires an active account before starting or loading a game. Open the account menu, select a saved student or teacher account, or create one. Account creation asks for a username and password; an account can also have a recovery code. The account menu can switch accounts, show account details, and remove an account.

### Start or resume

- **Play / new game:** resets the active account's playthrough, starts the tutorial, and opens the map.
- **Load game:** loads the selected account's progress and returns to its saved campaign scene. If tutorial completion is not recorded, the tutorial starts again.
- **Options:** opens the settings view, including audio controls where connected in the scene.
- **Credits / quit:** opens credits or closes the application in a built player.

Starting a new game resets completed-level and tutorial progress for that account. It does not mean that other accounts' progress is reset.

### Map, campaign, and level selection

The map provides campaign/module information panels and campaign navigation. Tutorial selection is enforced while the tutorial requires it; the Rizal campaign selection is blocked until that requirement is complete. Level selection buttons open the selected level, while locked/completed presentation is driven by account progress and the level assets.

Campaigns and levels are data-driven. `BackgroundSet` supplies campaign identity, quiz resources, and level-specific scenery/ground materials. `LevelData` specifies the level name, scene, required number of battles, campaign, heroes, possible enemies, and links to previous/next levels. The project contains Tutorial, Rizal, and Lapu-Lapu campaign assets and multiple level assets; which items are reachable depends on the configured maps and scene buttons.

### Battle loop

1. The player turn begins with the configured action points (AP) and a refilled hand.
2. Click a card to inspect its full description. Confirm to play or cancel to return it to the hand.
3. A card spends its AP cost when accepted. If AP is insufficient, the game shows a warning and does not play the card.
4. Single-target cards ask for a valid enemy or hero target, depending on the effect. Area and self-targeting cards resolve directly.
5. After card play, choose **End Turn**. Enemies perform their actions, then a new player turn begins with refreshed AP and cards.
6. Defeat the required enemy groups to finish the level. If every hero falls, the battle is lost.

Card effects include damage, piercing damage (bypasses shields), bleed damage over time, healing, shields, buffs, and debuffs. Cards can target all enemies, a single target, all heroes, or the acting hero. Heroes and enemies have health and shield displays; effect icons and combat text communicate active effects and results. Enemy behavior is selected from configured attack, heal, shield, buff, and debuff actions with per-enemy action weights.

The battle log records card and enemy actions. Character animations, hit/death feedback, card hover/transition animations, battle sounds, and AP display support the combat UI.

### Quiz cards

Playing a quiz card pauses the battle and opens a random question from the active quiz set. Questions can be text, image, or audio. Answer formats are multiple choice, multiple select, identification, true/false, and fill-in-the-blank. Audio questions provide a playback control. Multiple-select answers must match the complete correct-answer set. Text entry is trimmed and compared without case sensitivity.

After submission, the game highlights correct and incorrect answers and may show a linked information panel (title, image, and explanation). Quiz totals and correct answers are recorded overall and by answer format. A correct answer activates the quiz card. An incorrect answer leaves the card effect inactive and gives enemies a penalty turn; the player's remaining AP and hand are preserved. Quiz questions are randomly selected, so a question may recur.

At the end of a level, victory/defeat screens show quiz totals broken down by answer format. Victory offers return, retry, and continue where a next level exists. Defeat offers the available retry/return flow. The tutorial can temporarily restrict some battle and victory actions until its steps are complete.

### Pause and navigation

Press **Esc** during battle to pause or resume. The pause menu can resume, exit to the last level-selection screen, or quit the application. The victory return action also returns to the last level-selection screen. These routes have a Rizal level-select fallback for direct scene testing.

## 3. Tutorial, story, and learning content

The tutorial is a staged guide that tracks tutorial completion and coordinates map, battle, and victory instructions. A spotlight/mask can focus attention on a UI element, and tutorial battle rules can constrain which cards or actions are available at each step. New-game and loaded-progress flows decide whether to start, resume, or skip it.

Cutscenes are authored as ordered dialogue lines. A line can use a hero, enemy, or narrator; character lines can set a name, side, and pose (idle, talking, thinking, surprised, happy, angry, or sad). The cutscene manager displays dialogue, spawns/framed character models, advances on user input, and restores the game UI when the scene ends. Level data associates start, after-battle, and end cutscenes with trigger points.

Module information panels show a configured title, description, and image. The reusable scrollable-content data supports styled text and images, image placement above/below/hidden, explicit/native/panel-width sizing, aspect-ratio preservation, padding, and spacing.

## 4. Accounts, classrooms, and teacher tools

### Account types

The account model has **Student** and **Teacher** roles. Account cards are selectable, and login checks the password. Password visibility controls are available on login/create forms. Recovery uses a generated recovery code to set a new password. Usernames, password hashes, recovery hashes, account role, classroom, and teacher-assigned student IDs are represented in account data.

The account UI supports creating and deleting accounts, selecting the active account, login, recovery, and viewing student/teacher information. Teacher views can display assigned students and their progress/quiz metrics. Teachers can select students and confirm assignment to their teacher account. Student detail views expose completed-level and quiz-stat summaries.

### Classrooms

The active classroom is selected from Firestore-backed classroom records or created by name. Switching classroom updates the current database context, downloads that classroom's accounts, refreshes the account UI, and handles pending account deletion recovery. A valid active classroom is required for quiz package authoring and synchronization.

### Online account/progress services

Firebase Authentication and Firestore are used for online services. Account and progress operations include downloading/uploading accounts, teacher/student assignments, uploading/downloading campaign progress, and recording quiz results. Local pending-operation records support retry/reconciliation when sync is interrupted. Firebase/network availability and correct service configuration are required for cloud operations; local play/progress files are also used.

## 5. Quiz Maker and quiz packages

### Create and edit a package

The Quiz Maker stores packages beneath the active classroom folder in the app's persistent data location. Creating a package makes a folder with a quiz text file, companion `_info.txt` file, and `Images` and `Sounds` folders. Package names must be valid and unique within that classroom.

Select a package, then add questions by entering the prompt, choosing a display type (text/image/audio) and response type, and supplying the answer data. Multiple-choice requires four answer options and one correct answer. Multiple-select requires four options and at least two correct options. Identification and fill-in-the-blank require a correct text answer; fill-in-the-blank requires exactly one `____` marker. True/false records the correct boolean. Image/audio prompts can attach media from the file picker.

The editor can list/select questions, delete selected questions, remove a package (with confirmation), and select/deselect the package for play. Deleting a synced package coordinates remote deletion state with local cleanup. Closing the editor can synchronize pending package work.

### Quiz info / explanations

Quiz Info Maker creates or edits an entry associated with each question ID. Entries can contain a title, image path, and explanatory text. After an answer, this extra context can appear in the quiz info panel. This is optional; missing info does not prevent a question from being loaded.

### Upload, download, and update

Quiz package sync uses Firestore, the active classroom, Firebase authentication, and a selected local package. Upload publishes a version of the package; downloads discover published packages and can install or update a local copy. Local sync metadata records the package's classroom/version relationship. The uploader checks for version conflicts and validates integrity using file hashes; the package upload limit is 20 MiB. Package data is chunked for storage, and incomplete/pending updates have recovery handling. Deleted or incomplete remote versions are excluded from normal package discovery.

For a custom quiz to be used in battle, select it in the Quiz Maker before entering the game flow that loads the quiz. If no custom package is selected, or it has no usable questions, the active campaign's quiz is used as the fallback.

## 6. Progress and statistics

Progress is associated with the active account. It tracks completed levels, tutorial completion, the last campaign scene, lifetime question/correct counts by input format, and pending quiz results for synchronization. Local JSON progress is written under Unity's persistent data path; Firestore is used for cloud progress when configured. On load, the project reconciles local and cloud state and preserves unsent quiz results during sync recovery.

Within a level, victory can require multiple battles as defined by `battlesRequired`. Completing the requirement records the level and opens the victory screen. The level assets link to the next level where one exists. A retry reloads the current level; continue loads the linked next level or returns to selection when the chain ends.

Quiz statistics are also kept for the current level/session and displayed on the result screen: multiple choice, identification, true/false, multiple select, fill-in-the-blank, and total correct/questions.

## 7. Audio, visual, and interaction features

- Scene transitions support fade effects and optional scene-ready synchronization.
- Main-menu presentation includes a shuffled card/deck animation and backdrop handling.
- Background tiles and ground materials can vary by campaign/level; foliage sway scripts animate environmental objects.
- Sound handlers cover scene music, button sounds, card sounds, battle sounds, and answer feedback. Volume groups can be set and persisted.
- Cards, account entries, enemy/hero panels, effect indicators, and battle log are generated or refreshed from game state.
- Input is primarily mouse-driven UI and target selection, with **Esc** as the battle pause shortcut.

## 8. Project content and editor support

The project includes data assets for heroes (Rizal, Lapu-Lapu, Teodora, Paciano, Blumentritt), enemies (including soldier, priest, general, conquistador), cards, levels, campaigns, cutscenes, modules, and quizzes. These ScriptableObject assets let the game content be configured separately from the battle and UI code.

An editor inspector for card assets supports authoring card data. Debug/test helpers exist for damage, Firestore operations, battle logs, and quiz package discovery/download/upload/recovery. These are development support features and are not part of the normal player flow. Third-party asset and package folders (Firebase, External Dependency Manager, Standalone File Browser, Sketchfab importer, fonts, models, textures, and animations) provide integrations or content; they are not separate in-game modes.

## 9. Feature map by system

| System | Main responsibilities |
| --- | --- |
| Main menu and scene transitions | Play/load, options, credits, quit, campaign navigation, fades, ready signals |
| Account manager | Student/teacher accounts, login, recovery, selection, deletion, teacher/student views and assignment |
| Classroom and Firebase managers | Classroom selection/creation, account/progress/assignment/quiz package cloud access |
| Progress manager | Per-account level/tutorial progress, local saves, cloud sync, quiz-result queue and reconciliation |
| Level and campaign managers | Level setup, required battle count, level completion, victory/defeat, retry/continue |
| Battle controller | Turns, AP, enemy AI/actions, card resolution, targeting, win/loss, battle log |
| Cards and combat UI | Card data/effects/targets, hand draw/refill, preview/confirmation, hero/enemy panels, effect indicators |
| Quiz manager and Quiz Maker | Question presentation/scoring, text/image/audio questions, five answer formats, authoring and quiz info |
| Package upload/download/sync | Classroom package publishing, install/update, integrity checks, deletion and recovery |
| Tutorial and cutscenes | Staged onboarding, guided battle actions, dialogue, character staging and poses |
| Presentation and audio | Map/background motion, animation relays, transitions, sound effects, volume settings |

## 10. Practical setup notes

- Use a Unity Editor version compatible with `ProjectSettings/ProjectVersion.txt` and open the project through `Barahang-Bayani.slnx` or Unity Hub.
- Cloud classrooms, accounts, progress, and quiz packages need Firebase configuration and an authenticated session. A missing/offline backend can prevent those online actions.
- Quiz package features require a selected non-default classroom. The local package is selected separately from the campaign quiz fallback.
- Exact button visibility and accessibility depend on scene/prefab references. This document describes implemented code/data capabilities, not a guarantee that every feature is exposed in every build.
- `isBossLevel` is marked unused in level data. The project has boss-related content assets, but that flag does not currently drive a distinct boss-level flow.
