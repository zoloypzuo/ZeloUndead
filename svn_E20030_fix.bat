rem NOTE: in order to solve the following question:
rem svn: E200030: sqlite[S13]: database or disk is full, executing statement 'VACUUM '

sqlite3 .svn\wc.db "reindex nodes"
sqlite3 .svn\wc.db "reindex pristine"
pause complete
