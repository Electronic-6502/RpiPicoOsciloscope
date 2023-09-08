A very simple osciloscope, uses RPI pico as data acusation (DAQ) unit, and wpf for displaying signal

Downloadable Releases:
- Proof of Concept: [DOWNLOAD](https://github.com/epsi1on/SimpleOscilloscope/releases/download/POC/release.zip)

Here is output for osciloscope for a PWM signal with 25% duty cycle

![Screen Shot](POC.gif?raw=true "Screnshot")

# How to start

0- Download rp2daq.uf2 from rp2dac project: [DOWNLOAD](https://github.com/FilipDominec/rp2daq/raw/main/build/rp2daq.uf2)

1- unplug RPI pico from PC, hold the button on the RPI PICO and while pressing it down, connect the usb cable to PICO. copy the rp2daq.uf2 file into the drive shown in the explorer. keyword for search: "upload uf2 file to raspberrypi pico"

2- Download POC version of osciloscope from here [DOWNLOAD](https://github.com/epsi1on/SimpleOscilloscope/releases/download/POC/release.zip), unzip.

3- Run the binary file `SimpleOsciloscope.UI.exe` file from zipped file, select the COM port of RPI pico and choose a samplingrate (default is 500'000 which is 500K sample per second) and click the connect button.

hopefully everything works as expected and you'll see the waveform on the screen. PWM signals are tested and works good, maybe on more complex waveform application have some bugs, as it is proof of concept still.

Please report issues in the Issue section.