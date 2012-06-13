﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reality.Storage;
using System.Data;

namespace Reality.Game.Misc.Chat
{
    public static class Wordfilter
    {
        public static List<String> BlockedWords
        {
            get
            {
                return mBlockedWords;
            }
        }

        private static List<String> mBlockedWords;
        public static void Initialize(SqlDatabaseClient MySqlClient)
        {
         
            ReloadCache(MySqlClient);
        }

        public static void ReloadCache(SqlDatabaseClient MySqlClient)
        {
            mBlockedWords = new List<String>();
            DataTable words = MySqlClient.ExecuteQueryTable("SELECT word FROM wordfilter");
            foreach (DataRow Row in words.Rows)
            {
                if(!mBlockedWords.Contains((string)Row["word"])) {
                    mBlockedWords.Add((string)Row["word"]);
                }
            }
        }

        public static String Filter(String MessageText)
        {
            String[] Filter = MessageText.Split(' ');
            MessageText = "";
            foreach (String word in Filter)
            {
                if (MessageText != "")
                {
                    MessageText += " ";
                }
                if (Wordfilter.BlockedWords.Contains(word))
                {
                    for (int i = 0; i < word.Length; i++)
                    {
                        MessageText += "*";
                    }
                }
                else
                {
                    MessageText += word;
                }
            }
            return MessageText;
        }
    }
}