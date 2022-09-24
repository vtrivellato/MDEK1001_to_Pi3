-- SQLite

/*CREATE TABLE pos_register 
(
    id INTEGER PRIMARY KEY AUTOINCREMENT, 
    tag TEXT, 
    pos_X DECIMAL(5, 2), 
    pos_Y DECIMAL(5, 2), 
    pos_Z DECIMAL(5, 2), 
    ins_date DATETIME, 
    sent BOOLEAN DEFAULT FALSE
);*/

SELECT * 
FROM pos_register 
ORDER BY 1 DESC;