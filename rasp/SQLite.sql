-- SQLite

--drop table pos_register;

/*create table pos_register 
(
    id integer primary key autoincrement, 
    tag text, 
    posX decimal(5, 2), 
    posY decimal(5, 2), 
    posZ decimal(5, 2), 
    data DATETIME, 
    enviado boolean
);*/

select * 
from pos_register 
order by 1 desc;

insert into pos_register (tag, posX, posY, posZ, data, enviado)
values ('C406', 11.1, 21.0, 31.0, datetime('now', 'localtime'), false);

--update pos_register set enviado = 0 where id = 1;