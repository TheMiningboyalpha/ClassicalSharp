// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.Entities;
using ClassicalSharp.Entities.Mobs;
using ClassicalSharp.Gui.Widgets;
using ClassicalSharp.Gui.Screens;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Timers;
using ClassicalSharp.Particles;



#if USE16_BIT
using BlockID = System.UInt16;
#else
using BlockID = System.Byte;
#endif

namespace ClassicalSharp.Mode {

    public sealed class SurvivalGameMode : IGameMode {

        Game game;
        int score = 0;
        internal byte[] invCount = new byte[Inventory.BlocksPerRow * Inventory.Rows];
        Random rnd = new Random();
        public SurvivalGameMode() { invCount[8] = 10; } // tnt

        public bool HandlesKeyDown(Key key) { return false; }

        System.Timers.Timer BreakTimer = new System.Timers.Timer();
        Vector3I pos;
        BlockID oldblock;
        float blockhp = 10;
        SoundType blocksound;
        int oldwidth;
        int oldlength;

        public void PickLeft(BlockID old) {
            if (BreakTimer.Enabled == false)
            {

                GetBlockHp(old);
                pos = game.SelectedPos.BlockPos;
                oldblock = old;
                BreakTimer.Interval = 100;
                BreakTimer.Enabled = true;


            }

            game.AudioPlayer.PlayStepSound(blocksound);
        }

        public void GetBlockHp(BlockID block) //breaking sound too
        {
            if (block == Block.Dirt)
            {

                blockhp = 4;
                blocksound = SoundType.Grass;

            }
            else if (block == Block.Grass)
            {

                blockhp = 4;
                blocksound = SoundType.Grass;
            }
            else if (block == Block.Stone)
            {

                blockhp = 50;
                blocksound = SoundType.Stone;

            }
            else if (block == Block.CoalOre)
            {

                blockhp = 55;
                blocksound = SoundType.Stone;
            }
            else if (block == Block.IronOre)
            {

                blockhp = 60;
                blocksound = SoundType.Stone;
            }
            else if (block == Block.GoldOre)
            {

                blockhp = 65;
                blocksound = SoundType.Stone;
            }
            else if (block == Block.Cobblestone)
            {

                blockhp = 50;
                blocksound = SoundType.Stone;
            }
            else if (block == Block.Log)
            {

                blockhp = 10;
                blocksound = SoundType.Wood;
            }
            else if (block == Block.Wood)
            {

                blockhp = 10;
                blocksound = SoundType.Wood;
            }
            else if (block == Block.Sand)
            {

                blockhp = 2;
                blocksound = SoundType.Sand;
            }
            else if (block == Block.Gravel)
            {

                blockhp = 2;
                blocksound = SoundType.Gravel;
            }
            else if (block == Block.Leaves)
            {

                blockhp = 1;
                blocksound = SoundType.None;
            }
            else if (block == Block.TNT)
            {

                blockhp = 0;
                blocksound = SoundType.None;
            }
            else
            {
                blockhp = 1;
                blocksound = SoundType.None;
            }
        }

        public void BlockBreaking(object source, ElapsedEventArgs e)
        {

            blockhp--;
            if (blockhp <= 0)
            {
                game.UpdateBlock(pos.X, pos.Y, pos.Z, 0);
                game.UserEvents.RaiseBlockChanged(pos, oldblock, 0);
                HandleDelete(oldblock);
                BreakTimer.Enabled = false;
                blockhp = 10;

            }
        }

        public void PickMiddle(BlockID old) {
        }

        public void PickRight(BlockID old, BlockID block) {
            int index = game.Inventory.SelectedIndex, offset = game.Inventory.Offset;
            if (invCount[offset + index] == 0) return;

            Vector3I pos = game.SelectedPos.TranslatedPos;
            game.UpdateBlock(pos.X, pos.Y, pos.Z, block);
            game.UserEvents.RaiseBlockChanged(pos, old, block);

            invCount[offset + index]--;
            if (invCount[offset + index] != 0) return;

            // bypass HeldBlock's normal behaviour
            game.Inventory[index] = Block.Air;
            game.Events.RaiseHeldBlockChanged();
        }

        public bool PickEntity(byte id) {

            mobsAttTimer.Enabled = false;
            mobsAttTimer.Enabled = true;

            float deltaX = game.LocalPlayer.Position.X - game.Entities[id].Position.X;
            float deltaY = game.LocalPlayer.Position.Y - game.Entities[id].Position.Y;
            float deltaZ = game.LocalPlayer.Position.Z - game.Entities[id].Position.Z;

            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

            if (game.Entities[id].ModelName == "humanoid") { }
            else
            {
                if (distance < 4)
                {
                    if (game.Entities[id].Health > 1)
                    {
                        game.Entities[id].Health -= 1;
                        game.Entities[id].Velocity = CalculVelocityMobs(game.Entities[id].Velocity, id);
                       // game.Chat.Add("MOB: " + id + "," + game.Entities[id].ModelName + " HP:" + game.Entities[id].Health);

                       

                    }
                    else
                    {
                        if (game.Entities[id].ModelName == "sheep" || game.Entities[id].ModelName == "pigs")
                        {
                            score = score + 10;
                        }
                        if (game.Entities[id].ModelName == "zombie")
                        {
                            score = score + 80;
                        }
                        if (game.Entities[id].ModelName == "spider")
                        {
                            score = score + 105;
                        }
                        if (game.Entities[id].ModelName == "skeleton")
                        {
                            score = score + 120;
                        }
                        if (game.Entities[id].ModelName == "creeper")
                        {
                            score = score + 200;
                        }

                        UpdateScore();
                        game.Entities[id] = null;
                    }
                }
            }
            return true;
        }



        public void UpdateScore()
        {
            game.Chat.Add("&fScore: &e" + score, MessageType.Status1);
            return;
        }

        System.Timers.Timer mobsAttTimer = new System.Timers.Timer();
        System.Timers.Timer FallingDamTimer = new System.Timers.Timer();
        System.Timers.Timer LeftClickTimer = new System.Timers.Timer();
        System.Timers.Timer myTimer4 = new System.Timers.Timer();

        public void InitTimer()
        {
            BreakTimer.Elapsed += new ElapsedEventHandler(BlockBreaking);

            mobsAttTimer.Elapsed += new ElapsedEventHandler(MobsAttacks);
            mobsAttTimer.Interval = 300;
            mobsAttTimer.Enabled = true;

            FallingDamTimer.Elapsed += new ElapsedEventHandler(FallingDamage);
            FallingDamTimer.Interval = 50;
            FallingDamTimer.Enabled = true;

            LeftClickTimer.Elapsed += new ElapsedEventHandler(isleftclickdown);
            LeftClickTimer.Interval = 100;
            LeftClickTimer.Enabled = true;

        }

        public void isleftclickdown(object source, ElapsedEventArgs e)
        {
            if (game.Input.IsMousePressed(MouseButton.Left) == false)
            {

                BreakTimer.Enabled = false;
            }

            if (game.Input.IsMousePressed(MouseButton.Right) == true)
            {
                int index = game.Inventory.SelectedIndex;

                if (invCount[index] > 0)
                {
                    
                    useItem(game.Inventory[index],index);
                    
                }
            }

            if (pos != game.SelectedPos.BlockPos) {
                BreakTimer.Enabled = false;
            }
        }

        private void MobsAttacks(object source, ElapsedEventArgs e)
        {


            for (byte id = 0; id < ((int)((game.World.Length + game.World.Width / 2) / 5)); id++)
                if (game.Entities[id] == null) { } else {
                    {
                        if (game.Entities[id].ModelName == "zombie" || game.Entities[id].ModelName == "skeleton" || game.Entities[id].ModelName == "spider" || game.Entities[id].ModelName == "creeper")
                        {
                            if (game.LocalPlayer.Position.X < game.Entities[id].Position.X + 1)
                            {
                                if (game.LocalPlayer.Position.X > game.Entities[id].Position.X - 1)
                                {
                                    if (game.LocalPlayer.Position.Z < game.Entities[id].Position.Z + 1)
                                    {
                                        if (game.LocalPlayer.Position.Z > game.Entities[id].Position.Z - 1)
                                        {
                                            if (game.LocalPlayer.Position.Y < game.Entities[id].Position.Y + 1)
                                            {
                                                if (game.LocalPlayer.Position.Y > game.Entities[id].Position.Y - 1)
                                                {

                                                    game.LocalPlayer.Health -= 1;
                                                    game.LocalPlayer.Velocity = CalculVelocityPlayer(game.LocalPlayer.Position, id);
                                                    
                                                }

                                            }

                                        }

                                    }

                                }

                            }
                        }


                    }
                }

            verifyplayerhp();


        }


        public Widget MakeHotbar() { return new SurvivalHotbarWidget(game); }

        Vector3 CalculVelocityMobs(Vector3 old, byte id)
        {
            Vector3 newvel = old;

            if (game.Entities[id].Position.X < game.LocalPlayer.Position.X)
            {
                newvel.X = -0.3f;
            }
            else
            {
                newvel.X = 0.3f;
            }

            if (game.Entities[id].Position.Z < game.LocalPlayer.Position.Z)
            {
                newvel.Z = -0.3f;
            }
            else
            {
                newvel.Z = 0.3f;
            }

            newvel.Y = 0.4f;

            return newvel;
        }

        Vector3 CalculVelocityPlayer(Vector3 old, byte id)
        {
            Vector3 newvel = old;

            if (game.Entities[id].Position.X < game.LocalPlayer.Position.X)
            {
                newvel.X = 0.3f;
            }
            else
            {
                newvel.X = -0.3f;
            }

            if (game.Entities[id].Position.Z < game.LocalPlayer.Position.Z)
            {
                newvel.Z = 0.3f;
            }
            else
            {
                newvel.Z = -0.3f;
            }

            newvel.Y = 0.4f;

            return newvel;
        }

        void HandleDelete(BlockID old) {
            if (old == Block.Log) {
                AddToHotbar(Block.Wood, rnd.Next(3, 6));
            } else if (old == Block.CoalOre) {
                AddToHotbar(Block.Slab, rnd.Next(1, 4));
            } else if (old == Block.IronOre) {
                AddToHotbar(Block.Iron, 1);
            } else if (old == Block.GoldOre) {
                AddToHotbar(Block.Gold, 1);
            } else if (old == Block.Grass) {
                AddToHotbar(Block.Dirt, 1);
            } else if (old == Block.Stone) {
                AddToHotbar(Block.Cobblestone, 1);
            } else if (old == Block.Leaves) {
                if (rnd.Next(1, 16) == 1) { // TODO: is this chance accurate?
                    AddToHotbar(Block.Sapling, 1);
                }
            }
            else if (old == Block.TNT)
            {
            }else {
                AddToHotbar(old, 1);
            }
        }

        void AddToHotbar(BlockID block, int count)
        {
            int counter = 0;
            while (counter < 9)
            {

                if (game.Inventory[counter] == block && invCount[counter] < 99)
                {

                    invCount[counter] += (byte)count;
                    break;

                }


                if (game.Inventory[counter] == Block.Air)
                {

                    game.Inventory[counter] = block;
                    invCount[counter] += (byte)count;
                    break;
                }
                counter = counter + 1;
            }
        }

        float lastPositionY = 0f;
        public static float FallDistance = 0f;
        public static bool grounded = false;

        private void FallingDamage(object source, ElapsedEventArgs e)
        {
            grounded = false;
            if (grounded)
            {
                FallDistance = 0;
                lastPositionY = 0;
            }
            else
            {
                if (lastPositionY > game.LocalPlayer.Position.Y)
                {
                    FallDistance += lastPositionY - game.LocalPlayer.Position.Y;

                }
                lastPositionY = game.LocalPlayer.Position.Y;
            }

            int BlockX = (int)game.LocalPlayer.Position.X;
            int BlockY = (int)game.LocalPlayer.Position.Y - 1;
            int BlockZ = (int)game.LocalPlayer.Position.Z;

            if (game.World.GetBlock(BlockX, BlockY, BlockZ) == Block.Air) { } else
            if (game.World.GetBlock(BlockX, BlockY, BlockZ) == Block.Water || game.World.GetBlock(BlockX, BlockY + 1, BlockZ) == Block.Lava || game.World.GetBlock(BlockX, BlockY + 1, BlockZ) == Block.StillWater || game.World.GetBlock(BlockX, BlockY + 1, BlockZ) == Block.StillLava) {
                FallDistance = 0;
                lastPositionY = game.LocalPlayer.Position.Y;
            }
            else
            {

                if (FallDistance > 3)
                {

                    for (int i = 0; i < FallDistance - 3; i++)
                    {
                        game.LocalPlayer.Health -= 1;


                    }
                    verifyplayerhp();
                    
                }

                grounded = true;
                FallDistance = 0;




            }

        }

        void verifyplayerhp()
        {

            if (game.LocalPlayer.Health < 1)
            {
                Vector3 Newpos;
                Newpos.X = (float)game.World.Width / 2;
                Newpos.Z = (float)game.World.Length / 2;

                Newpos = Respawn.FindSpawnPosition(game, Newpos.Z, Newpos.X, game.LocalPlayer.Size);

                LocationUpdate update = LocationUpdate.MakePos(Newpos, false);
                game.LocalPlayer.SetLocation(update, false);
                game.LocalPlayer.Health = 20;
                game.Chat.Add("You died.");

            }

        }

        public void useItem(BlockID type,int index)
        {
            BlockID block = type;

            if (block == Block.RedMushroom) { 
                if (game.LocalPlayer.Health < 20)
                    {
                        game.LocalPlayer.Health += 4;

                    invCount[index]--;
                    if (invCount[index] <= 0)
                    {
                        game.Inventory[index] = Block.Air;
                        game.Events.RaiseHeldBlockChanged();
                    }

                    
                }   
                   }

            else if (block == Block.BrownMushroom) { 
                    if (game.LocalPlayer.Health < 20)
                    
                        {
                            game.LocalPlayer.Health += 4;

                            invCount[index]--;
                             if (invCount[index] <= 0)
                             {
                                 game.Inventory[index] = Block.Air;
                                 game.Events.RaiseHeldBlockChanged();
                             }
                    
                        }
                            
                    }

           if (game.LocalPlayer.Health > 20)
            {
                game.LocalPlayer.Health = 20;
            }

        }

        public void OnNewMapLoaded(Game game) {

            for (int i = 0; i < ((int)((oldlength + oldwidth / 2) / 5)); i++)
            {
                game.Entities[i] = null;
            }

            game.Chat.Add("&fScore: &e" + score, MessageType.Status1);
            string[] models = { "sheep", "pig", "skeleton", "zombie", "creeper", "spider" };
            for (int i = 0; i < ((int)((game.World.Length + game.World.Width / 2) / 5)); i++) {
                MobEntity fail = new MobEntity(game, models[rnd.Next(models.Length)]);
                float x = rnd.Next(0, game.World.Width) + 0.5f;
                float z = rnd.Next(0, game.World.Length) + 0.5f;

                Vector3 pos = Respawn.FindSpawnPosition(game, x, z, fail.Size);
                fail.SetLocation(LocationUpdate.MakePos(pos, false), false);
                game.Entities[i] = fail;
                game.Entities[i].Health = 10;


            }
            oldwidth = game.World.Width;
            oldlength = game.World.Length;

        }

        public void Init(Game game) {



            this.game = game;
            BlockID[] hotbar = game.Inventory.Hotbar;
            for (int i = 0; i < hotbar.Length; i++)
                hotbar[i] = Block.Air;
            hotbar[Inventory.BlocksPerRow - 1] = Block.TNT;
            game.Server.AppName += " (survival)";
            InitTimer();

           
        }


        public void Ready(Game game) { }
        public void Reset(Game game) { }
        public void OnNewMap(Game game) { }
        public void Dispose() { }

       


}
}
