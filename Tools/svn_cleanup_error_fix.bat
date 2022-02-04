rem fix svn cleanup error
rem Failed to run the WC DB work queue associated

cd ..
sqlite3 .svn/wc.db "select * from work_queue"
sqlite3 .svn/wc.db "delete from work_queue"
sqlite3 .svn/wc.db "select * from work_queue"
pause complete