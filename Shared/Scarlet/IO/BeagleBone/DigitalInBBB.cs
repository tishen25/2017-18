﻿using BBBCSIO;
using System;

namespace Scarlet.IO.BeagleBone
{
    public class DigitalInBBB : IDigitalIn
    {
        public BBBPin Pin { get; private set; }
        private Port InputPort;

        private InterruptPortMM IntPortRise, IntPortFall, IntPortAny;
        private event EventHandler<InputInterrupt> RisingHandlers;
        private event EventHandler<InputInterrupt> FallingHandlers;
        private event EventHandler<InputInterrupt> AnyHandlers;

        public DigitalInBBB(BBBPin Pin)
        {
            this.Pin = Pin;
        }

        public bool GetInput()
        {
            if (BeagleBone.FastGPIO) { return ((InputPortMM)this.InputPort).Read(); }
            else { return ((InputPortFS)this.InputPort).Read(); }
        }

        public void Initialize()
        {
            if (BeagleBone.FastGPIO) { this.InputPort = new InputPortMM(IO.BeagleBone.Pin.PinToGPIO(this.Pin)); }
            else { this.InputPort = new InputPortFS(IO.BeagleBone.Pin.PinToGPIO(this.Pin)); }
        }

        public void RegisterInterruptHandler(EventHandler<InputInterrupt> Handler, InterruptType Type)
        {
            switch (Type)
            {
                case InterruptType.RISING_EDGE:
                {
                    if (this.IntPortRise == null)
                    {
                        this.IntPortRise = new InterruptPortMM(IO.BeagleBone.Pin.PinToGPIO(this.Pin), InterruptMode.InterruptEdgeLevelHigh);
                        this.IntPortRise.EnableInterrupt();
                        this.IntPortRise.OnInterrupt += this.InterruptRising;
                    }
                    this.RisingHandlers += Handler;
                    return;
                }
                case InterruptType.FALLING_EDGE:
                {
                    if (this.IntPortFall == null)
                    {
                        this.IntPortFall = new InterruptPortMM(IO.BeagleBone.Pin.PinToGPIO(this.Pin), InterruptMode.InterruptEdgeLevelLow);
                        this.IntPortFall.EnableInterrupt();
                        this.IntPortFall.OnInterrupt += this.InterruptFalling;
                    }
                    this.FallingHandlers += Handler;
                    return;
                }
                case InterruptType.ANY_EDGE:
                {
                    if (this.IntPortAny == null)
                    {
                        this.IntPortAny = new InterruptPortMM(IO.BeagleBone.Pin.PinToGPIO(this.Pin), InterruptMode.InterruptEdgeBoth);
                        this.IntPortAny.EnableInterrupt();
                        this.IntPortAny.OnInterrupt += this.InterruptAny;
                    }
                    this.AnyHandlers += Handler;
                    return;
                }
            }
        }

        internal void InterruptRising(GpioEnum EventPin, bool EventState, DateTime Time, EventData Data)
        {
            InputInterrupt Event = new InputInterrupt();
            this.RisingHandlers?.Invoke(this, Event);
        }

        internal void InterruptFalling(GpioEnum EventPin, bool EventState, DateTime Time, EventData Data)
        {
            InputInterrupt Event = new InputInterrupt();
            this.FallingHandlers?.Invoke(this, Event);
        }

        internal void InterruptAny(GpioEnum EventPin, bool EventState, DateTime Time, EventData Data)
        {
            InputInterrupt Event = new InputInterrupt();
            this.AnyHandlers?.Invoke(this, Event);
        }

        public void SetResistor(ResistorState Resistor)
        {
            throw new NotImplementedException();
            // TODO: Does this actually require device tree changes? O.o
        }

        public void Dispose()
        {
            if(BeagleBone.FastGPIO)
            {
                ((InputPortMM)this.InputPort).ClosePort();
                ((InputPortMM)this.InputPort).Dispose();
            }
            else
            {
                ((InputPortFS)this.InputPort).ClosePort();
                ((InputPortFS)this.InputPort).Dispose();
            }
            this.IntPort.DisableInterrupt();
            this.IntPort.ClosePort();
            this.IntPort.Dispose();
            this.IntPort = null;
        }
    }
}
