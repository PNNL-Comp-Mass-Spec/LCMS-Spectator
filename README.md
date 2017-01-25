# LCMS-Spectator
LCMS raw data and MSGF+ results viewer

LcmsSpectator is a standalone Windows graphical user interface tool for viewing LC-MS data and identifications.

It takes a single or multiple Thermo raw files (\*.raw) or MzML file and shows MS and MS/MS spectra as well as precursor and product ion chromatograms as resizable, floatable tabbed-documents.

Identification and target files can also be loaded to show the spectral and chromatographic evidence. The tool currently supports MS-GF+ and MSPathFinder tab-delimited results, and MzIdentML files.

It uses [Informed-Proteomics](https://github.com/PNNL-Comp-Mass-Spec/Informed-Proteomics) for reading spectrum files and can function as a GUI for ProMex and MSPathFinder.

## Downloads

* https://github.com/PNNL-Comp-Mass-Spec/LCMS-Spectator/releases
* https://omics.pnl.gov/software/lcmsspectator

### Supported file formats
#### Spectrum files

When used with no other software installed, LcmsSpectator supports the use of centroid mzML files as spectrum input. If Thermo Finnigan MSFileReader is installed, it also supports reading from Thermo Finnigan .raw files ([Download here](https://thermo.flexnetoperations.com/control/thmo/download?element=6306677), requires registration to download).

Several other formats are supported if an appropriate version of ProteoWizard is installed ([Download here](http://proteowizard.sourceforge.net/downloads.shtml), make sure the version downloaded matches system architecture)

#### Search result files
Supports .mzid files, but will only show limited information about the results for .mzid files that were not produced by MSGF+ or MSPathFinder (scores will not be shown for other search tools).

Also supports .tsv files created by MSPathFinder or MSGF+.

## System Requirements
Minimum required:
* .NET 4.5.1 installed
* Will probably try to run on anything where you can install .NET 4.5.1.

Minimum recommended:
* 2.4 GHz, quad-core CPU
* 16 GB RAM
* Windows 7 or newer
* 250 GB hard drive

Website: http://omics.pnl.gov/

-------------------------------------------------------------------------------

Licensed under the Educational Community License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
http://www.osedu.org/licenses/ECL-2.0

Unless required by applicable law or agreed to in writing,
software distributed under the License is distributed on an "AS IS"
BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
or implied. See the License for the specific language governing
permissions and limitations under the License.
