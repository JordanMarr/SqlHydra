--------------------------------------------------------------------------------------
-- Name	       : OT (Oracle Tutorial) Sample Database
-- Link	       : http://www.oracletutorial.com/oracle-sample-database/
-- Version     : 1.0
-- Last Updated: July-28-2017
-- Copyright   : Copyright © 2017 by www.oracletutorial.com. All Rights Reserved.
-- Notice      : Use this sample database for the educational purpose only.
--               Credit the site oracletutorial.com explitly in your materials that
--               use this sample database.
--------------------------------------------------------------------------------------
--------------------------------------------------------------------
-- execute the following statements to create a user name OT and
-- grant priviledges
--------------------------------------------------------------------

ALTER SESSION SET CONTAINER = XEPDB1;

-- create new user
CREATE USER OT IDENTIFIED BY Oracle1;

-- grant priviledges
GRANT CONNECT, RESOURCE, DBA TO OT;
GRANT CREATE SESSION TO OT;

COMMIT;

/
show errors

prompt ot_create_user complete!
