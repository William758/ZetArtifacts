# ZetArtifacts

Can configure artifacts to be disabled or always active.

Artifact of the Bazaar - The Bazaar Between Time contains a larger variety of interactables.

- Disabled by default. The purpose of this artifact is to toggle other bazaar mod content.

- Relies on other mods to actually populate the bazaar. (like BiggerBazaar and BazaarExpand)

Artifact of Revival - Dead players respawn after the boss is defeated.

Artifact of Tossing - Allows players to drop and scrap items. (LeftAlt + RMB to scrap)

Artifact of Multitudes - Double player count scaling. (stacks multiplicatively with original multitudes mod)

Artifact of Sanction - Monster and Interactable types can appear earlier.

Artifact of Accumulation - Collected items increase difficulty. Interactable spawns are increased.

Artifact of Escalation - Post-loop Elites begin to appear at monster level 10. Monster spawns are increased.

Artifact of the Eclipse - Enables all Eclipse modifiers.

## Installation:

Requires Bepinex and R2API.

Use r2modman or place inside of Risk of Rain 2/Bepinex/Plugins/

## Changelog:

v1.4.7 - Accumulation display now includes time added by Puzzle of Chronos from MysticsItems. Config to add scrapper to bazaar while using Artifact of Tossing.

v1.4.6 - Added config for LeftAlt requirement for Tossing. Fix NRE when handling buffs.

v1.4.5 - Added Blighted Elites to Escalation. Escalation has a chance to reroll T1 Elites into early T2 Elites.

v1.4.4 - Added Aragonite Elites to Escalation.

v1.4.3 - Fixed Artifact of the Bazaar preventing BazaarIsMyHome from spawning lunar pods.

v1.4.2 - Separate Tossing configs for all void item tiers.

v1.4.1 - Added Tossing configs for all item tiers. Update Accumulation display on scene change.

v1.4.0 - Added Artifacts: Bazaar, Sanction, Accumulation.

v1.3.5 - Apply EarlyElite properties earlier so LittleGameplayTweaks doesn't break.

v1.3.4 - Config to prevent command essence when dropping items. Drop items towards look direction.

v1.3.3 - Fixed for latest update. Re-enabled Escalation early elites.

v1.3.2 - EclipseArtifacts compatibility.

v1.3.1 - Disable Escalation elites until EliteAPI is fixed.

v1.3.0 - Updated for latest game version. Fix Safer Spaces cooldown buff handling. Multitudes Fix increases MaxMonsterSpawnCount for CombatShrines.

v1.2.4 - Config to disable dropping WorldUnique items. Config to remove scrappers while using Artifact of Tossing. Prevent scrapping ArtifactKey. Null-check impaleDotIndexField.

v1.2.3 - Configs for some artifact values. Scale Erythrite DOT based on ambient level. Don't populate Escalation EliteDef until run starts.

v1.2.2 - Configure artifacts to be disabled or set always active.

v1.2.1 - Switched networking from MiniRPCLib to R2API. Clients now show item drop notification instead of only host seeing all of them. Artifact of Revival only triggers if there is still a living player.

v1.2.0 - Added Artifact of Escalation.

v1.1.0 - Compatibility with DiluvianArtifact.

v1.0.0 - Initial Release.

## Credits:

Artifact of Multitudes is based off of wildbook's [Multitudes](https://thunderstore.io/package/wildbook/Multitudes/).

Artifact of Tossing is based off of [KookehsDropItemMod](https://thunderstore.io/package/tristanmcpherson/KookehsDropItem_BepInEx/)
