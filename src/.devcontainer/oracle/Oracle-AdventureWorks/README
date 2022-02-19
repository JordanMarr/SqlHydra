2009/12/19

Thank you for downloading the Oracle Essbase Studio Tutorial.




This download provides the following main files and core directories:
-----------------------------------------------
/DLLConstructs/
/DataInserts/
AdvWorks_Create_User.sql
AdvWorks_Build_Data.sql




Instructions (Windows):
-----------------------------------------------
1. 	Open Command-Line Prompt



2. 	Change the directory to the directory under which you have unzipped the 
	download (ex: cd c:\downloads\oracle_essbase_studio_tutorial\). 
	
	In the example given, this _Read_Me.txt file should be in the directory "oracle_essbase_studio_tutorial".



3.	Enter SQLPLUS in the command-line to start SQL*PLUS.

	
	EXAMPLE:
	-----------------------------
	C:\downloads\oracle_essbase_studio_tutorial> SQLPLUS /nolog




4.	Connect using a SysDBA account.

	
	EXAMPLE:
	-----------------------------
	SQL> Connect system as sysdba
	...
	...(when prompted for the password, enter the password, and hit the enter key)

	

	NOTE:
	-----------------------------
	This script seeks to create a new user called AdvWorks issued by a user 
	with privileges to the sysdba role. If you desire to integrate the 
	objects provided in this tutorial with an existing user scheme then please simply run the DLL and DAL scripts 
	provided in the /DLLConstructs and /DataInserts folders under the scheme you have in mind. In which case, 
	there is no need to run the main script in Step 5.  Skip Step 5 an Proceed to Step 6 Note b.




5.	Run the initial user/scheme creation and priviliges script by entering the following in the command-line:


	SQL> @AdvWorks_Create_User_Script.sql;

	
	Press the Enter key to Execute the script.


	NOTE:
	-----------------------------
	Ensure than when you are typing the above syntax in the command line that you are entering the "@" symbol 
	as shown. This symbol denotes the running of a script. If you are unfamiliar with the use of this symbol 
	please consult the Oracle documentation.




6.	In Step 5 the user scheme was created along 
	with its privileges (Privileges are for example only and do not mimic a production environment).

	Enter the following in the SQLPLUS command-line to build the table objects and load data:


	SQL> @AdvWorks_Build_Data.sql;



	NOTE:
	-----------------------------
	a.) This may take several minutes (sub 8 minutes on most machines).

	b.) If you are attempting to implement this tutorial's data with a scheme different then the one to 
	    intended for creation in Step 5, then prior to running this script "AdvWorks_Build_Data.sql", open this 
	    file and change the value for the session variable in the line "alter session set current_schema" 
	    to the one which you have in mind.  Save the file and then run the script.  
	    Be sure that your desired scheme has appropriate privileges.
	
	c.) The script is complete when the message "SCRIPT COMPLETE" is displayed.



================================================
ADDITIONAL NOTES
================================================

I.	If there is a need to drop the AdvWorks user and all tables, run the following command when connected as the sysdba role:

	DROP USER AdvWorks CASCADE;
















