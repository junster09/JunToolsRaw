# JUN TOOLS

## Overview
**A destructive toolset for generating VRChat avatar animators.**

It's a handy toolset I made for myself since I follow a similar engineering scheme for a lot of my avatars. Because I like being able to edit animators afterwards, the tools make permanent changes to the animator. If you don't like fiddling with animators in detail; I do not recommend usage of these tools. I would rather recommend something like VRCFury or Modualar Avatar.

## Install

Since these are the RAW files, just import the repo to the assets folder. No VCC compatibility yet. You should find "Tools > JunTools" when properly imported

## Emotion Set Setup
Allow the user to setup emotion states quickly.

## Selector Setup
Allows the user to create selectors for toggles, with support for substating.

# TODO
**[Emotion Set Setup]** Allow asymetrical hand gestures

**[Emotion Set Setup]** refactor code to be less messy

**[Selector Setup]** Switch the [-]/[+] buttons

**[Selector Setup]** Add a "substate sync", upon adding a substate and parameter, press a button to "sync" state names with the layer that has said substate parameter.

**[Selector Setup]** Add a safety check, so that the tool doesn't destroy and regenerate an already generated animator tree; but instead changes or adds any difference.

**[Selector Setup] [Emotion Set Setup]** Add an "export prefrence" so that the user can just load in a setup; instead of manually putting it in each time.

**[Menu Setup]** make a menu setup for VRChat quick menus. 