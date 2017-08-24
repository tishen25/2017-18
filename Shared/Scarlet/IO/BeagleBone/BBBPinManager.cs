﻿using Scarlet.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Scarlet.IO.BeagleBone
{
    public static class BBBPinManager
    {
        private static Dictionary<BBBPin, PinAssignment> Mappings;

        public static void AddMapping(BBBPin SelectedPin, bool IsOutput, ResistorState Resistor, BBBPinMode PinMode, bool FastSlew = true)
        {
            if (Mappings == null) { Mappings = new Dictionary<BBBPin, PinAssignment>(); }
            byte Mode = Pin.GetModeID(SelectedPin, PinMode);
            if (Mode == 255) { throw new InvalidOperationException("This type of output is not supported on this pin."); }
            if (!Pin.CheckPin(SelectedPin, BeagleBone.Peripherals)) { throw new InvalidOperationException("This pin cannot be used without disabling some peripherals first."); }
            PinAssignment NewMap = new PinAssignment(SelectedPin, Pin.GetPinMode(FastSlew, !IsOutput, Resistor, Mode));
            lock(Mappings)
            {
                if (Mappings.ContainsKey(SelectedPin))
                {
                    Log.Output(Log.Severity.WARNING, Log.Source.HARDWAREIO, "Overriding pin setting. This may mean that you have a pin usage conflict.");
                    Mappings[SelectedPin] = NewMap;
                }
                else { Mappings.Add(SelectedPin, NewMap); }
            }
        }

        /// <summary>
        /// Generates the device tree file, compiles it, and instructs the kernel to load the overlay though the cape manager. May take a while.
        /// Currently this can only be done once, as Scarlet does not have a way of removing the existing mappings.
        /// </summary>
        public static void ApplyPinSettings()
        {
            if(Mappings.Count == 0) { Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "No pins defined, skipping device tree application."); return; }
            string FileName = "Scarlet-DeviceTree.dts";
            StreamWriter DTOut = new StreamWriter(FileName);
            List<string> DeviceTree = GenerateDeviceTree();
            foreach (string Line in DeviceTree) { DTOut.WriteLine(Line); }
            DTOut.Flush();
            DTOut.Close();
        }

        private class PinAssignment
        {
            public BBBPin Pin { get; private set; }
            public byte Mode { get; private set; }

            public PinAssignment(BBBPin Pin, byte Mode)
            {
                this.Pin = Pin;
                this.Mode = Mode;
            }
        }

        static List<string> GenerateDeviceTree()
        {
            List<string> Output = new List<string>();

            Output.Add("/dts-v1/;");
            Output.Add("/plugin/;");
            Output.Add("");
            Output.Add("/ {");
            Output.Add("        /* Generated by Scarlet. */");
            Output.Add("        compatible = \"ti,beaglebone\", \"ti,beaglebone-black\";");
            Output.Add("        part-number = \"scarlet-pins\";");
            Output.Add("        version = \"00A1\";");
            Output.Add("        ");
            Output.Add("        fragment@0{");
            Output.Add("                target = <&am33xx_pinmux>;");
            Output.Add("                __overlay__ {");
            Output.Add("                        scarlet_pins: scarlet_pin_set {");
            Output.Add("                                pinctrl-single,pins = <");

            lock (Mappings)
            {
                foreach (PinAssignment PinAss in Mappings.Values)
                {
                    string Offset = String.Format("0x{0:X3}", (Pin.GetOffset(PinAss.Pin) - 0x800));
                    string Mode = String.Format("0x{0:X2}", PinAss.Mode);
                    if (Offset != "0x000") { Output.Add("                                        " + Offset + " " + Mode); }
                }
            }

            Output.Add("                                >;");
            Output.Add("                        };");
            Output.Add("                };");
            Output.Add("        };");
            Output.Add("        ");
            Output.Add("        fragment@1{");
            Output.Add("                target = <&ocp>;");
            Output.Add("                __overlay__ {");
            Output.Add("                        test_helper: helper {");
            Output.Add("                                compatible = \"bone-pinmux-helper\";");
            Output.Add("                                pinctrl-names = \"default\";");
            Output.Add("                                pinctrl-0 = <&scarlet_pins>;");
            Output.Add("                                status = \"okay\";");
            Output.Add("                        };");
            Output.Add("                };");
            Output.Add("        };");
            Output.Add("};");

            return Output;
        }
    }
}
