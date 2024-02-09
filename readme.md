[![GitHub release (version 1.2.1)](https://img.shields.io/badge/Executables-v2.1.1-blue)](https://github.com/HansBilliet/CockpitHardwareHUB_v2/tree/master/Executables)
[![GitHub release (version 1.2.0.0)](https://img.shields.io/badge/WASM--Module-v1.2.0.0-blue)](https://github.com/HansBilliet/CockpitHardwareHUB_v2/tree/master/WASM%20Module)
[![GitHub release (version 1.2.1)](https://img.shields.io/badge/Documentation-v2.1.1-blue)](https://github.com/HansBilliet/CockpitHardwareHUB_v2/tree/master/CockpitHardwareHUB%20User%20Manual.pdf)

# CockpitHardwareHUB_v2

<p align="center">
  <img src="assets/CockpitHardwareHUB_v2.png" alt="CockpitHardwareHUB_v2 Logo">
</p>

## Introduction
**CockpitHardwareHUB_v2** is a tool designed for cockpit builders to connect hardware to Microsoft Flight Simulator 2020. Unlike other tools that require specific configurations or are limited to certain microcontroller brands, CockpitHardwareHUB_v2 offers flexibility and broad compatibility.

### Key Features
- Compatibility with any microcontroller providing a serial interface via USB.
- Configurations are "pushed" from each device, eliminating extra maintenance.
- Devices are hot swappable

### Concept
The tool listens for `Win32_SerialPort` devices and identifies hardware using the PNPDevice string format `USB\VID_vvvv&PID_pppp\nnnnnnn`. The string components include USB indication, VendorID, ProductID, and a unique serial number for each device. The tool allows to filter on specific VID, PID and/or Serial Number.

## Registration Process
Devices undergo a registration process involving command-response sequences, such as:
- `RESET`: Puts device in Registration Mode.
- `IDENT`: Device responds with DeviceName and ProcessorType.
- `REGISTER`: Device sends its Property strings.

## Properties and Variables
Properties received are added to the Property Pool, becoming **Variables** registered with MSFS using `SimConnect` and/or the `WASM module`. Properties are identified by a **Property Id** which is the sequence in which they are sent starting from 1. Variables get their own **Variable Id** which is managed by CockpitHardwarHUB_v2.

### Construction of a Device Property
Device properties follow a specific format, including:
- **ValueType**: Type of the property's value.
- **RW**: Read or Write indication.
- **VarType**: Type of variable (Simulation, Local, Events, etc.)
- **VarName**: Name of the variable.
- **Index** (optional): Additional variable information.
- **Unit**: (Only for 'A' type variables)

### Data Exchange
Once Properties are successfully registered and linked with a Variable, data can be exchanged between MSFS and the devices. For efficiency, the 3-digit Property ID is used rather than the full Property string. Each command has the format **NNN[=[data]]**, in which **NNN** is the Property ID and **data** the optional data (if the variable requires it), prepended by the **=**-sign.

CockpitHardwareHUB_v2 applies restrictions based on ValueType, RW and VarType.

## User Interface
The tool assists in developing hardware with features like:
- `execute_calculator_code`: Helps in experimenting with commands.
- Virtual Device: Simulates a real USB device for property testing.
- Overview of all registered Variables, and in case of 'R'ead variables real-time update of the values.

## Installation

The GitHub repository contains 2 folders that allow to immediately use the application:
- [Executables](https://github.com/HansBilliet/CockpitHardwareHUB_v2/tree/master/Executables) - Copy this to a folder on your computer
- [WASM module](https://github.com/HansBilliet/CockpitHardwareHUB_v2/tree/master/WASM%20Module) - The folder 'wasimcommander-module' needs to be copied in the Community folder of MSFS 2020

## Credits
CockpitHardwareHUB_v2 is developed by Hans Billiet. The project leverages contributions from Maxim Paperno who is the creator of [WASimCommander](https://github.com/mpaperno/WASimCommander), and Paul van Dinther for the module [SerialPortManager.cs](https://github.com/dinther/SerialPortManager).

CockpitHardwarHUB_v2 uses the WASimCommander WASM module and a slightly adapted version of WASimCommander.WASimClient.dll (v1.2.0.1). Both are included in this repository. SerialPortManager.cs has been adapted to allow it to find a broader range of devices.

## Copyright License and Disclaimer
- Copyright: Hans Billiet; All Rights Reserved.
- Licensed under GNU General Public License (GPL) v3 or later.
- WASimCommander project is used under the terms of the GPLv3 license
- No warranty for the program's utility, suitability, or merchantability.

---

[View User Manual](CockpitHardwareHUB%20User%20Manual.pdf)
