# LCMS-Spectator

LCMS raw data and MSGF+ results viewer

LCMS-Spectator is a standalone Windows program for viewing LC-MS data and peptide identifications.

It reads Thermo raw files (*.raw) and mzML files and shows MS and MS/MS spectra as well as precursor and product ion chromatograms as resizable, floatable tabbed-documents.

Identification and target files can also be loaded to show the spectral and chromatographic evidence. The tool currently supports [MS-GF+](https://github.com/MSGFPlus/msgfplus) MzIdentML files (.mzid) and [MSPathFinder](https://github.com/PNNL-Comp-Mass-Spec/Informed-Proteomics) tab-delimited result files (.tsv).

It uses [Informed-Proteomics](https://github.com/PNNL-Comp-Mass-Spec/Informed-Proteomics) for reading spectrum files and can function as a GUI for ProMex and MSPathFinder.

A guide for using LCMS-Spectator can be found on our [GitHub Wiki Page](https://github.com/PNNL-Comp-Mass-Spec/LCMS-Spectator/wiki).

## Downloads

* https://github.com/PNNL-Comp-Mass-Spec/LCMS-Spectator/releases

### Continuous Integration

The latest version of the application may be available on the [AppVeyor CI server](https://ci.appveyor.com/project/PNNLCompMassSpec/lcms-spectator/build/artifacts),
but builds are deleted after 6 months. \
[![Build status](https://ci.appveyor.com/api/projects/status/bw5slqcrbg923gfu?svg=true)](https://ci.appveyor.com/project/PNNLCompMassSpec/lcms-spectator)

### Supported file formats
#### Spectrum files

LCMS-Spectator supports the use of centroided mzML files as spectrum input. It can also directly read Thermo .Raw files using the bundled Thermo RawFileReader library.

Several other formats are supported if an appropriate version of ProteoWizard is installed ([Download here](https://proteowizard.sourceforge.io/download.html); make sure the version downloaded matches system architecture, typically Windows 64-bit)

#### Search result files

Supports .mzid files, but will only show limited information about the results for .mzid files that were not produced by MS-GF+ or MSPathFinder (scores will not be shown for other search tools).

Also supports .tsv files created by MSPathFinder or MS-GF+.

## System Requirements
Minimum required:
* .NET 4.7.2

Minimum recommended:
* 2.4 GHz, quad-core CPU
* 16 GB RAM
* Windows 10 or newer
* 250 GB hard drive

## Running MSPathFinder from LCMS Spectator

The suggested method for running MSPathFinder is to use the console version, available at https://github.com/PNNL-Comp-Mass-Spec/Informed-Proteomics/releases
* This has the advantage that you can see all of the log messages shown at the console.

However, LCMS Spectator also supports running MSPathFinder, as outlined here:

1) Convert your data file to a centroided .mzML file\
`msconvert.exe --32 --mzML --filter "peakPicking true 1-" DatasetName.raw`

2) Start LCMS Spectator and choose the Search menu

3) Define the Dataset Info
* Spectrum File Path: path to the .mzML file
* Fasta DB File Path: path to the protein sequence FASTA file
* Feature File path: leave blank
* Output Directory: will be auto-defined as the directory with the .mzML file, but can be changed

4) Define the search settings
* Default settings are shown; customize if desired
* If you specify a custom scan range, the features file (.ms1ft) will not be created, and the viewing of MS/MS spectra will show all data points, not just peaks, which can greatly slow down the viewer

5) Select the proteins to search
* By default, selects all proteins in the FASTA file

6) Define dynamic and/or static modifications

7) Click "Run"
* A progress dialog will appear, but will stay blank while the .mzML file is being processed to create the .pbf file
* Next, a new FASTA file is created based on the specified FASTA file
* It will include the proteins you seleced on the Sequence tab
* It tracks the protein name and the first word of the protein description, plus the residues of each protein

8) The FASTA file will be indexed, creating files .icanno, .icseq, and .icplcp

9) The target (forward) sequence database will be searched

10) The decoy (shuffled) sequence database will be searched

11) When finished, E-values are computed and results written, creating a .tsv file named DatasetName_IcDecoy.tsv

## Contacts

Written by Chris Wilkins and Bryson Gibbons for the Department of Energy (PNNL, Richland, WA) \
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov \
Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics

## License

Licensed under the Educational Community License, Version 2.0 (the "License");
you may not use this file except in compliance with the License. You may obtain 
a copy of the License at http://www.osedu.org/licenses/ECL-2.0

Unless required by applicable law or agreed to in writing,
software distributed under the License is distributed on an "AS IS"
BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
or implied. See the License for the specific language governing
permissions and limitations under the License.

Copyright 2018 Battelle Memorial Institute

RawFileReader reading tool. Copyright � 2016 by Thermo Fisher Scientific, Inc. All rights reserved.
