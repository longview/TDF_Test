# About
This is a basic [TDF162](https://en.wikipedia.org/wiki/TDF_time_signal) demodulator using an I/Q FM discriminator. The TDF 162 signal is a French long wave radio clock signal transmitted using an unusual phase modulation scheme.
The signal contains (among other things) a stable time code, and the average frequency of the transmitter is apparently accurate to within around 10<sup>-12</sup>. 

It seems a good receiver can keep the second phase error below 200 µs. The seemingly official status/logging page is here: [SERVICE DES REFERENCES NATIONALES DE TEMPS](https://syrte.obspm.fr/tfc/temps/outgoing_data/ALS162/).

Note that in French, the signal is referred to as ALS162, and while there still isn't an enormous amount of public documentation at least there is _some_ in French.

![Screenshot of program](screenshot.PNG?raw=true "Screenshot")

This is the tech-demo for a potential embedded TDF162 radio clock.

If you're just looking for a demodulator, there aren't too many SDR decoders, but the [SDRangel Radio clock plugin](https://github.com/f4exb/sdrangel/blob/master/plugins/channelrx/radioclock/readme.md) is one that looks like it should work.

# Principle of Operation

The input is a 16 bit mono wave file at 20 kHz sample rate containing a recording of TDF162 containing one full minute (i.e. from :00 to :59 or longer) detected using a wide-USB detector tuned to 157 kHz

A few wave files are included that I used for verification, a webSDR recording is the reference signal since it has ridiculously high SNR. The other recordings are made using my receiver system in Norway.

The demodulator performs a complex downconversion to baseband, then low pass filters (100 element moving average) and decimates to 200 Hz.
A basic non-atan() differentiator based detector is used for performance reasons.

The resulting demodulated FM is integrated to make a PM demodulator, though this is not used. 
The output of this detector is intended to drive a local oscillator (not implemented here since it's a batch processor). An attempt was made to calculate the FM error based on the PM data, doesn't work right yet.


The FM signal is low pass filtered for further noise reduction using another simple moving average filter. We also rectify and low pass filter the FM detector signal even more for use in the synchronization function.

In order to decode data, the FM and FM rectified signals are used. 
Three (least square error) correlators are used, these look for the start of a minute using the rectified data (this one is just zeros),
    the waveform of a binary 0, and the waveform of a binary 1.

The minute start correlator may be slightly too noise sensitive, and could perhaps be replaced with a "smart" algorithm of some sort (i.e. just threshold the rectified data and count the zeros to sync).

The correlator waveforms are extracted from the included webSDR recording, and output to .txt files in the working dir (this happens every time you run it). The values can be pasted into the appropriate cs file.

Work is in progress on alternate correlator techniques, including convulational detectors, and synthetic correlator reference generators.

After computation, a search is made for the minute start by simply finding the peak of the correlator 3 array.
When this is found, the search window for correlation is narrowed, and the first binary 0 is located initially.
(This is guaranteed to be 0)

Once the first 0 is found, a fairly narrow time-window is searched to find the remaining 58 bits.

Note that since the binary '1' waveform has better autocorrelation properties and a longer template, so it tends to give better SNR even for binary '0'. Because of this the '0' correlator (correlator1) is weighted based on the ratio of template length and what the '1' correlator (correlator2) outputs for the known-0 first bit in a minute.

After this, some decoding is done to display the results, including detectable bit errors.

The program crashes if it finds the "wrong" start of minute (in case there are multiples), since it likely won't have enough bits after reaching the end of the file.

The intent is to implement this functionality in a real-time processor (i.e. STM32F3), obviously some changes are needed before that can happen.
However, it does prove that a receiver is viable, and that my local reception is adequate for detection.

The same principles could also be used to implement a DCF77 phase mode receiver (using the spread spectrum modulation it transmits).

The program now also includes a timecode bitstream generator which allows more precise bit-error detection, and could be used to build a full-chain system simulator.

# Use
The program outputs to a console, as you'd expect. I use a VS 2019 extension called _ArrayPlotter_ to view the internal data structures, the array *decimated_sampleperiod* can be used as a X-scale.
 
Note that the time provided is the time that _will_ occur when the next minute starts, not the time of the currently decoded minute.

Note also that a timing system using this system will likely need to operate with a coherent receiver (i.e. actually implement the phase feedback loop), and because of how much non-time (non-documented, at least in English) data is transmitted it's definitely not easy to open loop track time pulses.

Basically I suspect the receiver will need to keep very good time between each minute pulse to be able to track the signal reliably since the false-alarm rate of using only the time-data per second is not good. This is because there is so much non-time data transmitted in between the timeslots for time data.

If I were prone to wild guesses, I'd note that a transmitter of this type _could_ serve as a decent backup submarine communications transmitter. France operates transmitters in the more common 15-40 kHz VLF band, so this would obviously not be a primary use case.