import json
import os

FILE_PATH = "Data/Config/keybinds.json"

if not os.path.exists(FILE_PATH):
    print("keybinds.json not found! Run the game once to generate it.")
    exit(1)

with open(FILE_PATH, "r") as f:
    binds = json.load(f)

# Group keys by command and context
grouped = {}
for bind in binds:
    cmd = bind["CommandId"]
    ctx = bind["Context"]
    key = bind["Key"]
    mod = bind["Modifiers"]
    
    label = f"{cmd} (Context: {ctx})"
    if label not in grouped:
        grouped[label] = []
        
    combo = key if mod == "None" else f"{mod}+{key}"
    grouped[label].append({"combo": combo, "raw": bind})

print("==== CURRENT KEYBINDS ====")
labels = list(grouped.keys())

for i, label in enumerate(labels):
    combos = [b["combo"] for b in grouped[label]]
    print(f"[{i}] {label}: {', '.join(combos)}")

print("\nEnter the number of the command to edit, or 'q' to quit:")
choice = input("> ")

if choice.lower() == 'q' or not choice.isdigit() or int(choice) >= len(labels):
    print("Exiting.")
    exit(0)

target_label = labels[int(choice)]
target_cmd = grouped[target_label][0]["raw"]["CommandId"]
target_ctx = grouped[target_label][0]["raw"]["Context"]

print(f"\nEditing {target_label}")
print("Enter comma-separated keys (e.g. 'W, UpArrow' or 'Shift+T'):")
new_keys_str = input("> ")

# Remove old binds for this command+context
binds = [b for b in binds if not (b["CommandId"] == target_cmd and b["Context"] == target_ctx)]

# Add new ones
import re
new_keys = [k.strip() for k in new_keys_str.split(",")]
for key_combo in new_keys:
    parts = key_combo.split("+")
    if len(parts) > 1:
        mod = parts[0].strip()
        key = parts[1].strip()
    else:
        mod = "None"
        key = parts[0].strip()
        
    binds.append({
        "Context": target_ctx,
        "Key": key,
        "Modifiers": mod,
        "CommandId": target_cmd
    })

with open(FILE_PATH, "w") as f:
    json.dump(binds, f, indent=2)

print("\nKeybinds updated successfully!")
