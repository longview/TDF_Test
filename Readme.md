# About
This is a basic TDF162 demodulator using an I/Q FM discriminator. The TDF 162 signal is a French long wave radio clock signal transmitted using an unusual phase modulation scheme.
The signal contains (among other things) a stable time code, and the average frequency of the transmitter is presumably kept accurate.

This is the tech-demo for a potential embedded TDF162 radio clock.

The input is a 16 bit mono wave file at 20 kHz sample rate containing a recording of TDF162 containing one full minute (i.e. from :00 to :59 or longer) detected using a wide-USB detector tuned to 157 kHz

A few wave files are included that I used for verification, a webSDR recording is the reference signal since it has ridiculously high SNR. The other recordings are made using my receiver system in Norway.

The demodulator performs a complex downconversion to baseband, then low pass filters and decimates to 200 Hz
A basic non-atan() based detector is used for performance reasons.

The result is integrated to make a PM demodulator, though this is not used. 
The output of this detector can be used to drive a local oscillator (not implemented here).

An attempt was made to calculate the FM error based on the PM data, doesn't work right yet.
The FM signal is low pass filtered for further noise reduction. We also rectify and low pass filter the FM detector signal for use later.

In order to decode data, the FM and FM rectified signals are used. 
Three correlators are used, these look for the start of a minute using the rectified data (this one is just zeros),
    the waveform of a binary 0, and the waveform of a binary 1.

The correlator waveforms are extracted from the included webSDR recording, and output to .txt files in the working dir.

After computation, a search is made for the minute start.
When this is found, the search window for correlation is narrowed, and the first binary 0 is located initially.
(This is guaranteed to be 0)

Once the first 0 is found, a fairly narrow time-window is searched to find the remaining 58 bits.

After this, some decoding is done to display the results, including detectable bit errors.

The program crashes if it finds the "wrong" start of minute (in case there are multiples), since it likely won't have enough bits after reaching the end of the file.

The intent is to implement this functionality in a real-time processor (i.e. STM32F3), obviously some changes are needed.
However, it does prove that a receiver is viable, and that my local reception is adequate for detection.

# Use
The program outputs to a console, as you'd expect. I use a VS 2019 extension called ArrayPlotter to view the internal data structures, the array *decimated_sampleperiod* can be used as a X-scale.
 