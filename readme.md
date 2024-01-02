
# CockpitHardwareHUB_v2

## Introduction
**CockpitHardwareHUB_v2** is a tool designed for cockpit builders to connect hardware to Microsoft Flight Simulator 2020. Unlike other tools that require specific configurations or are limited to certain microcontroller brands, CockpitHardwareHUB_v2 offers flexibility and broad compatibility.

### Key Features
- Compatibility with any microcontroller providing a serial interface via USB.
- Automatic configuration requirement "push" from each device, eliminating extra maintenance.

### Concept
The tool listens for `Win32_SerialPort` devices and identifies hardware using the PNPDevice string format `USB\VID_vvvv&PID_pppp
nnnnnnn`. The string components include USB indication, VendorID, ProductID, and a unique serial number for each device.

## Registration Process
Devices undergo a registration process involving command-response sequences, such as:
- `RESET
`: Puts device in Registration Mode.
- `IDENT
`: Device responds with DeviceName and ProcessorType.
- `REGISTER
`: Device sends its Property strings.

## Properties and Variables
Properties received are added to the Property Pool, becoming 'Variables' registered with MSFS using SimConnect and/or the WASM module. Variables are identified by 'Property Id' and translated into an internal numbering system.

### Construction of a Device Property
Device properties follow a specific format, including:
- ValueType: Type of the property's value.
- RW: Read or Write indication.
- VarType: Type of variable (Simulation, Local, Events, etc.)
- VarName: Name of the variable.
- Extension (optional): Additional variable information.
- Unit: (Only for 'A' type variables)

## Restrictions and Data Exchange
CockpitHardwareHUB_v2 applies restrictions based on VarType and facilitates data exchange between MSFS 2020 and the connected devices.

## User Interface
The tool assists in developing hardware with features like:
- Connection group: For managing connections with MSFS 2020 and devices.
- USB Devices: Shows properties of detected devices and allows interaction.
- Execute_calculator_code: Helps in experimenting with commands.
- Virtual Device: Simulates a real USB device for property testing.

## Credits
Developed by Hans Billiet, the project leverages contributions from Maxim Paperno (WASimCommander) and utilizes insights from ChatGPT 4.0.

## Copyright License and Disclaimer
- Copyright: Hans Billiet; All Rights Reserved.
- Licensed under GNU General Public License (GPL) v3 or later.
- No warranty for the program's utility, suitability, or merchantability.

---

[View the complete document](CockpitHardwareHUB%20Readme.docx)
