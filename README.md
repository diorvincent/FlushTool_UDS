CAN/LIN bus brush tool description


This tool is based on the development of CAN/LIN bus UDS, mainly used for PTC embedded program scrub, DTC read/clear, UDS service test, for scrub APP (.hex, .bin, .H86, .cbf) file basic information read.
This program is based on the hardware development of Tomoss UTA0402 and PCAN. If it is paired with other types of Tumos bus message adapters, you need to use the corresponding version of the library file to replace the two files USB2XXX.dll and libusb-1.0.dll in the installation directory to ensure normal use.


1. Connect the PTC and Tums bus adapter and start the 12V DC power supply. Run the program.

2. Click Initialize in the upper left corner of the screen to connect the program to the Tumos bus adapter.

3. On the Trace page, click Read/Clear in the DTC column to read or clear the fault diagnosis code.

4. Click Test in the Test Diag Service column to perform a diagnostic service test. The test sends a series of diagnostic service requests to the PTC through the Tumos adapter based on the diagnostic service template file in the installation directory to verify the working status of the PTC diagnostic services. You can modify the diagnostic service template file as required (back it up in advance).

5. Click Timestamp as period to display the test time in relative time format; otherwise, the system time will be displayed.

6. The Clear message list button is used to clear the message list on the interface.

7. Select the Downloader page, click the Browse button, and load the.hex file to be swiped (note: hex files need to be filled with multiple blocks using a tool like HexView,Generate 1 block file).

8. The Download button runs the scrub program. The interface is locked. The write progress is displayed at the bottom of the interface, and the packets sent by the scrub are displayed in the message bar. This process cannot be powered off or forcibly shut down,Otherwise, flushing fails.

9. The ReadHex button is used to read the information (start address, block length, end address) of the hex file to be swiped.

10. The Clear button is used to clear information at the bottom of the screen.


During the use, if you encounter problems, please send an email or instant message to find me to update.

Email: bosaidon@hotmail.com

 
