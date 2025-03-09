##########################
Frequently Asked Questions
##########################

.. contents::
   :local:
   
***********
General FAQ
***********

.. _faqAudioFormats:

Which audio formats are supported?
==================================

At the moment SegnoSharp only supports MP3 (``.mp3``) and FLAC (``.flac``).

Why do I need the BASS libraries?
=================================

In order to play back various media files, and have the ability to stream the result to a server, you need some libraries that allow this functionality.
After extensive research into various media libraries, BASS was chosen for being fairly easy to use, and with plenty of features.

So even if it requires the user to manually download some files it provides the best feature set for this application.

This might change in the future if other libraries with similar functionality are developed.

.. _faqBassRegistration:

Do I need to register BASS?
===========================

To quote `un4seen developments <https://www.un4seen.com/bass.html>`_:

.. epigraph::

   BASS is free for non-commercial use. If you are a non-commercial entity (eg. an individual) and you are not making any money from [the] product (through sales, advertising, etc) then you can use BASS in it for free. Otherwise [a] licences will be required.

SegnoSharp should function normally without a registration, but if you intend to use it for more than demonstrational purposes you should get a valid registration.

*****************
Behind the scenes
*****************

What does "SegnoSharp" mean?
============================

A `"segno" <https://en.wikipedia.org/wiki/Dal_segno>`_ is a musical term that is used in repetitions.
SegnoSharp is a music streamer that will play, and repeat, musical files.

A `"sharp" <https://en.wikipedia.org/wiki/Sharp_(music)>`_ is also a musical term, but it is also a part of the
name of the programming language used to create SegnoSharp: C# (pronounced See Sharp)

Hence SegnoSharp is a play on musical and programming terms, and the logo contains a stylized "segno" symbol
on top of a "sharp" symbol.

Why is the logo so yellow?
==========================

When SegnoSharp was first conceived it didn't have a proper name, but was developed under the title ``Whitestone Audio Streaming Project``.
This can be abbreviated to ``WASP``, and wasps are yellow and black.