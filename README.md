# Werks_Tally
![WerksTallyHeader](https://github.com/user-attachments/assets/5b23fdac-af13-4c66-8463-60c5664588da)
Utility that parses a facility log into the number of completed projects in the game Foxhole. Used for tracking contribution to the Foxhole war effort for facilities. Uses Tesseract OCR in order to read images.

# How to use:
1. Enter a facility log in Foxhole and take a screenshot of the in-game window using Windows Snipping tool or your other tool of choice.
2. Copy the resulting image into the textbox to the left. Repeat for all the facilities you wish to parse the Werks from. You can mouse-over the little picture preview to see what you copied.
![image](https://github.com/user-attachments/assets/6b1b9f7f-2f5e-4cbb-aad1-fe35a1ba5a08)
3. After checking the results, press the "Save data" button. The results are saved in a Txt file which is opened after saving, as well as a CSV file you can open with your sheet editor of choice. The files will be created next to the executable.

![image](https://github.com/user-attachments/assets/fd9a1b00-44b9-4e8c-b9a6-7fb07f4e20b5)
![image](https://github.com/user-attachments/assets/b64c4c1b-6c7c-497a-906b-701f3eedc574)

Note: The results are appended in the files, so if you save the same data multiple times, you will have duplicates.

In addition, the first time you run a program, it will create a config.ini file next to the executable. You can set a specific path for where you'd like the CSV file to be made and appended to, as well as switching OCR languages.
Do note that if you switch OCR languages, you need to download the appropriate language file from https://github.com/tesseract-ocr/tessdata and place it next to the executable.
However, currently the only supported language is English, due to how the parser reads the lines.
