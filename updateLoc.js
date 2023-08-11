// Copies the exported lang file from Distant Seas and edits the descriptions for translators
// Requires you to hit "Export lang" in the DEBUG > Misc tab
const fs = require("fs");
const path = require("path");

const locPath = process.argv[2];
const loc = JSON.parse(fs.readFileSync(locPath, "utf8"));

const newLoc = {};

for (const [key, value] of Object.entries(loc)) {
  const originalDescription = value.description;

  if (value.description === "AboutSection.Draw") {
    const notBullets = [
      "AboutSectionHeader",
      "AboutSectionVersion",
      "AboutSectionDescriptionStart"
    ];

    if (key.startsWith("AboutSectionButton")) {
      value.description =
        "Displayed on a clickable button at the bottom of the About tab.";
    } else if (notBullets.includes(key)) {
      value.description = "Displayed in the About tab.";
    } else {
      value.description = "Displayed in a bullet point in the About tab.";
    }
  }

  if (value.description.includes("SettingsSection")) {
    if (key.endsWith("Description")) {
      value.description =
        "Displayed in the Settings tab, as a help label next to a configurable option.";
    } else {
      value.description =
        "Displayed in the Settings tab - most likely a label accompanied by a configurable option.";
    }
  }

  const sections = [
    "AboutSection",
    "JournalSection",
    "LeaderboardSection",
    "ProgressSection",
    "SchedulesSection",
    "SettingsSection"
  ];
  if (sections.includes(key)) {
    value.description =
      "The name of a section, displayed on the left sidebar of the main window.";
  }

  if (key === "CommandHelpMessage") {
    value.description =
      "Displayed in the Plugin Installer as the description for the /pseas command.";
  }

  if (key.startsWith("Time")) {
    value.description =
      "A time of day. Displayed in the Schedules section, and on a tooltip in the overlay.";
  }

  if (key.startsWith("VoyageMissionType")) {
    value.description =
      "A type of fish, used in voyage missions. Displayed on a tooltip in the overlay.";
  }

  if (key.startsWith("SchedulesSection")) {
    value.description =
      "Displayed in the Schedules section, most likely on a table header.";
  }

  if (key.startsWith("RelativeDate")) {
    value.description =
      "Displayed in the Schedules section, representing a relative date. The input is a number.";
  }

  if (key === "ZoneText") {
    value.description =
      "Displayed in the overlay, to the right of the zone name. The input is a number between 1 and 3.";
  }
  if (key === "ZoneHoverText") {
    value.description =
      "Displayed in the overlay, when hovering over the current zone.";
  }

  if (key.startsWith("Overlay")) {
    if (key.startsWith("OverlayTooltip")) {
      value.description = "Displayed in a tooltip in the overlay.";
    } else {
      value.description = "Displayed in the overlay.";
    }
  }

  if (key === "AlarmMessage") {
    value.description = "Displayed in the chat window when the alarm goes off.";
  }

  if (["JournalActive", "JournalEnabled", "JournalDisabled"].includes(key)) {
    value.description = "Displayed at the top of the Journal section.";
  }

  if (key.startsWith("JournalSection")) {
    value.description =
      "Displayed in the Journal section, most likely on a button or table header.";
  }

  if (key.startsWith("ProgressSection")) {
    value.description =
      "Displayed in the Progress section, most likely on a table header.";
  }

  if (key.startsWith("LeaderboardSection")) {
    value.description = "Displayed in the Leaderboard section.";
  }

  if (value.description == originalDescription) {
    console.warn(`No description for ${key}`);
  }
  newLoc[key] = value;
}

fs.writeFileSync("./loc.json", JSON.stringify(newLoc, null, 2));
