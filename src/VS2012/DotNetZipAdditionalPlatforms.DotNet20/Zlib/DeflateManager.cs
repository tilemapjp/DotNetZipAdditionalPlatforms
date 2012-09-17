namespace DotNetZipAdditionalPlatforms.Zlib
{
    using System;
    using System.Globalization;
    using System.Runtime.CompilerServices;

    internal sealed class DeflateManager
    {
        internal ZlibCodec codecField;
        internal int distanceOffsetField;
        private static readonly string[] errorMessageField = new string[] { "need dictionary", "stream end", "", "file error", "stream error", "data error", "insufficient memory", "buffer error", "incompatible version", "" };
        internal int lengthOffsetField;
        private bool wantRfc1950HeaderBytesField = true;
        internal short bibufField;
        internal int bivalidField;
        internal short[] blcountField = new short[InternalConstants.MAX_BITS + 1];
        internal short[] bltreeField = new short[((2 * InternalConstants.BL_CODES) + 1) * 2];
        internal int blockStartField;
        private const int Buf_size = 0x10;
        private const int BUSY_STATE = 0x71;
        internal CompressionLevel compressionLevelField;
        internal CompressionStrategy compressionStrategyField;
        private Config configField;
        internal sbyte dataTypeField;
        private CompressFunc deflateFunctionField;
        internal sbyte[] depthField = new sbyte[(2 * InternalConstants.L_CODES) + 1];
        internal short[] dynDtreeField = new short[((2 * InternalConstants.D_CODES) + 1) * 2];
        internal short[] dynLtreeField = new short[HEAP_SIZE * 2];
        private const int DYN_TREES = 2;
        private const int END_BLOCK = 0x100;
        private const int FINISH_STATE = 0x29a;
        internal int hashBitsField;
        internal int hashMaskField;
        internal int hashShiftField;
        internal int hashSizeField;
        internal short[] headField;
        internal int[] heapField = new int[(2 * InternalConstants.L_CODES) + 1];
        internal int heapLenField;
        internal int heapMaxField;
        private const int HEAP_SIZE = ((2 * InternalConstants.L_CODES) + 1);
        private const int INIT_STATE = 0x2a;
        internal int inshField;
        internal int lastEobLenField;
        internal int lastFlushField;
        internal int lastLitField;
        internal int litBufSizeField;
        internal int lookAheadField;
        internal int matchAvailableField;
        internal int matchLengthField;
        internal int matchStartField;
        internal int matchesField;
        private const int MAX_MATCH = 0x102;
        private const int MEM_LEVEL_DEFAULT = 8;
        private const int MEM_LEVEL_MAX = 9;
        private const int MIN_LOOKAHEAD = ((MAX_MATCH + MIN_MATCH) + 1);
        private const int MIN_MATCH = 3;
        internal int nextPendingField;
        internal int optLenField;
        internal byte[] pendingField;
        internal int pendingCountField;
        private const int PRESET_DICT = 0x20;
        internal short[] prevField;
        internal int prevLengthField;
        internal int prevMatchField;
        private bool rfc1950BytesEmittedField = false;
        internal int staticLenField;
        private const int STATIC_TREES = 1;
        internal int statusField;
        private const int STORED_BLOCK = 0;
        internal int strstartField;
        internal Tree treeBitLengthsField = new Tree();
        internal Tree treeDistancesField = new Tree();
        internal Tree treeLiteralsField = new Tree();
        internal int wbitsField;
        internal int wmaskField;
        internal int wsizeField;
        internal byte[] windowField;
        internal int windowSizeField;
        private const int Z_ASCII = 1;
        private const int Z_BINARY = 0;
        private const int Z_DEFLATED = 8;
        private const int Z_UNKNOWN = 2;

        internal DeflateManager()
        {
        }

        private void _fillWindow()
        {
            int num;
            int num4;
        Label_0001:
            num4 = (this.windowSizeField - this.lookAheadField) - this.strstartField;
            if (((num4 == 0) && (this.strstartField == 0)) && (this.lookAheadField == 0))
            {
                num4 = this.wsizeField;
            }
            else if (num4 == -1)
            {
                num4--;
            }
            else if (this.strstartField >= ((this.wsizeField + this.wsizeField) - MIN_LOOKAHEAD))
            {
                int num2;
                Array.Copy(this.windowField, this.wsizeField, this.windowField, 0, this.wsizeField);
                this.matchStartField -= this.wsizeField;
                this.strstartField -= this.wsizeField;
                this.blockStartField -= this.wsizeField;
                num = this.hashSizeField;
                int index = num;
                do
                {
                    num2 = this.headField[--index] & 0xffff;
                    this.headField[index] = (num2 >= this.wsizeField) ? ((short) (num2 - this.wsizeField)) : ((short) 0);
                }
                while (--num != 0);
                num = this.wsizeField;
                index = num;
                do
                {
                    num2 = this.prevField[--index] & 0xffff;
                    this.prevField[index] = (num2 >= this.wsizeField) ? ((short) (num2 - this.wsizeField)) : ((short) 0);
                }
                while (--num != 0);
                num4 += this.wsizeField;
            }
            if (this.codecField.AvailableBytesIn != 0)
            {
                num = this.codecField.read_buf(this.windowField, this.strstartField + this.lookAheadField, num4);
                this.lookAheadField += num;
                if (this.lookAheadField >= MIN_MATCH)
                {
                    this.inshField = this.windowField[this.strstartField] & 0xff;
                    this.inshField = ((this.inshField << this.hashShiftField) ^ (this.windowField[this.strstartField + 1] & 0xff)) & this.hashMaskField;
                }
                if ((this.lookAheadField < MIN_LOOKAHEAD) && (this.codecField.AvailableBytesIn != 0))
                {
                    goto Label_0001;
                }
            }
        }

        internal void _InitializeBlocks()
        {
            int num;
            for (num = 0; num < InternalConstants.L_CODES; num++)
            {
                this.dynLtreeField[num * 2] = 0;
            }
            for (num = 0; num < InternalConstants.D_CODES; num++)
            {
                this.dynDtreeField[num * 2] = 0;
            }
            for (num = 0; num < InternalConstants.BL_CODES; num++)
            {
                this.bltreeField[num * 2] = 0;
            }
            this.dynLtreeField[END_BLOCK * 2] = 1;
            this.optLenField = this.staticLenField = 0;
            this.lastLitField = this.matchesField = 0;
        }

        private void _InitializeLazyMatch()
        {
            this.windowSizeField = 2 * this.wsizeField;
            Array.Clear(this.headField, 0, this.hashSizeField);
            this.configField = Config.Lookup(this.compressionLevelField);
            this.SetDeflater();
            this.strstartField = 0;
            this.blockStartField = 0;
            this.lookAheadField = 0;
            this.matchLengthField = this.prevLengthField = MIN_MATCH - 1;
            this.matchAvailableField = 0;
            this.inshField = 0;
        }

        private void _InitializeTreeData()
        {
            this.treeLiteralsField.dyn_tree = this.dynLtreeField;
            this.treeLiteralsField.staticTree = StaticTree.Literals;
            this.treeDistancesField.dyn_tree = this.dynDtreeField;
            this.treeDistancesField.staticTree = StaticTree.Distances;
            this.treeBitLengthsField.dyn_tree = this.bltreeField;
            this.treeBitLengthsField.staticTree = StaticTree.BitLengths;
            this.bibufField = 0;
            this.bivalidField = 0;
            this.lastEobLenField = 8;
            this._InitializeBlocks();
        }

        internal static bool _IsSmaller(short[] tree, int n, int m, sbyte[] depth)
        {
            short num = tree[n * 2];
            short num2 = tree[m * 2];
            return ((num < num2) || ((num == num2) && (depth[n] <= depth[m])));
        }

        internal void _tr_align()
        {
            this.send_bits(STATIC_TREES << 1, 3);
            this.send_code(END_BLOCK, StaticTree.lengthAndLiteralsTreeCodes);
            this.bi_flush();
            if ((((1 + this.lastEobLenField) + 10) - this.bivalidField) < 9)
            {
                this.send_bits(STATIC_TREES << 1, 3);
                this.send_code(END_BLOCK, StaticTree.lengthAndLiteralsTreeCodes);
                this.bi_flush();
            }
            this.lastEobLenField = 7;
        }

        internal void _tr_flush_block(int buf, int stored_len, bool eof)
        {
            int num;
            int num2;
            int num3 = 0;
            if (this.compressionLevelField > CompressionLevel.None)
            {
                if (this.dataTypeField == Z_UNKNOWN)
                {
                    this.set_data_type();
                }
                this.treeLiteralsField.build_tree(this);
                this.treeDistancesField.build_tree(this);
                num3 = this.build_bl_tree();
                num = ((this.optLenField + 3) + 7) >> 3;
                num2 = ((this.staticLenField + 3) + 7) >> 3;
                if (num2 <= num)
                {
                    num = num2;
                }
            }
            else
            {
                num = num2 = stored_len + 5;
            }
            if (((stored_len + 4) <= num) && (buf != -1))
            {
                this._tr_stored_block(buf, stored_len, eof);
            }
            else if (num2 == num)
            {
                this.send_bits((STATIC_TREES << 1) + (eof ? 1 : 0), 3);
                this.send_compressed_block(StaticTree.lengthAndLiteralsTreeCodes, StaticTree.distTreeCodes);
            }
            else
            {
                this.send_bits((DYN_TREES << 1) + (eof ? 1 : 0), 3);
                this.send_all_trees(this.treeLiteralsField.max_code + 1, this.treeDistancesField.max_code + 1, num3 + 1);
                this.send_compressed_block(this.dynLtreeField, this.dynDtreeField);
            }
            this._InitializeBlocks();
            if (eof)
            {
                this.bi_windup();
            }
        }

        internal void _tr_stored_block(int buf, int stored_len, bool eof)
        {
            this.send_bits((STORED_BLOCK << 1) + (eof ? 1 : 0), 3);
            this.copy_block(buf, stored_len, true);
        }

        internal bool _tr_tally(int dist, int lc)
        {
            this.pendingField[this.distanceOffsetField + (this.lastLitField * 2)] = (byte) (dist >> 8);
            this.pendingField[(this.distanceOffsetField + (this.lastLitField * 2)) + 1] = (byte) dist;
            this.pendingField[this.lengthOffsetField + this.lastLitField] = (byte) lc;
            this.lastLitField++;
            if (dist == 0)
            {
                this.dynLtreeField[lc * 2] = (short) (this.dynLtreeField[lc * 2] + 1);
            }
            else
            {
                this.matchesField++;
                dist--;
                this.dynLtreeField[((Tree.LengthCode[lc] + InternalConstants.LITERALS) + 1) * 2] = (short) (this.dynLtreeField[((Tree.LengthCode[lc] + InternalConstants.LITERALS) + 1) * 2] + 1);
                this.dynDtreeField[Tree.DistanceCode(dist) * 2] = (short) (this.dynDtreeField[Tree.DistanceCode(dist) * 2] + 1);
            }
            if (((this.lastLitField & 0x1fff) == 0) && (this.compressionLevelField > CompressionLevel.Level2))
            {
                int num = this.lastLitField << 3;
                int num2 = this.strstartField - this.blockStartField;
                for (int i = 0; i < InternalConstants.D_CODES; i++)
                {
                    num += (int) (this.dynDtreeField[i * 2] * (5L + Tree.ExtraDistanceBits[i]));
                }
                num = num >> 3;
                if ((this.matchesField < (this.lastLitField / 2)) && (num < (num2 / 2)))
                {
                    return true;
                }
            }
            return ((this.lastLitField == (this.litBufSizeField - 1)) || (this.lastLitField == this.litBufSizeField));
        }

        internal void bi_flush()
        {
            if (this.bivalidField == 0x10)
            {
                this.pendingField[this.pendingCountField++] = (byte) this.bibufField;
                this.pendingField[this.pendingCountField++] = (byte) (this.bibufField >> 8);
                this.bibufField = 0;
                this.bivalidField = 0;
            }
            else if (this.bivalidField >= 8)
            {
                this.pendingField[this.pendingCountField++] = (byte) this.bibufField;
                this.bibufField = (short) (this.bibufField >> 8);
                this.bivalidField -= 8;
            }
        }

        internal void bi_windup()
        {
            if (this.bivalidField > 8)
            {
                this.pendingField[this.pendingCountField++] = (byte) this.bibufField;
                this.pendingField[this.pendingCountField++] = (byte) (this.bibufField >> 8);
            }
            else if (this.bivalidField > 0)
            {
                this.pendingField[this.pendingCountField++] = (byte) this.bibufField;
            }
            this.bibufField = 0;
            this.bivalidField = 0;
        }

        internal int build_bl_tree()
        {
            this.scan_tree(this.dynLtreeField, this.treeLiteralsField.max_code);
            this.scan_tree(this.dynDtreeField, this.treeDistancesField.max_code);
            this.treeBitLengthsField.build_tree(this);
            int index = InternalConstants.BL_CODES - 1;
            while (index >= 3)
            {
                if (this.bltreeField[(Tree.bl_order[index] * 2) + 1] != 0)
                {
                    break;
                }
                index--;
            }
            this.optLenField += (((3 * (index + 1)) + 5) + 5) + 4;
            return index;
        }

        internal void copy_block(int buf, int len, bool header)
        {
            this.bi_windup();
            this.lastEobLenField = 8;
            if (header)
            {
                this.pendingField[this.pendingCountField++] = (byte) len;
                this.pendingField[this.pendingCountField++] = (byte) (len >> 8);
                this.pendingField[this.pendingCountField++] = (byte) ~len;
                this.pendingField[this.pendingCountField++] = (byte) (~len >> 8);
            }
            this.put_bytes(this.windowField, buf, len);
        }

        internal int Deflate(FlushType flush)
        {
            if (((this.codecField.OutputBuffer == null) || ((this.codecField.InputBuffer == null) && (this.codecField.AvailableBytesIn != 0))) || ((this.statusField == FINISH_STATE) && (flush != FlushType.Finish)))
            {
                this.codecField.Message = errorMessageField[4];
                throw new ZlibException(string.Format(CultureInfo.InvariantCulture, "Something is fishy. [{0}]", this.codecField.Message));
            }
            if (this.codecField.AvailableBytesOut == 0)
            {
                this.codecField.Message = errorMessageField[7];
                throw new ZlibException("OutputBuffer is full (AvailableBytesOut == 0)");
            }
            int num = this.lastFlushField;
            this.lastFlushField = (int) flush;
            if (this.statusField == INIT_STATE)
            {
                int num2 = (Z_DEFLATED + ((this.wbitsField - 8) << 4)) << 8;
                int num3 = ((int) (((int)this.compressionLevelField - 1) & 0xff)) >> 1;
                if (num3 > 3)
                {
                    num3 = 3;
                }
                num2 |= num3 << 6;
                if (this.strstartField != 0)
                {
                    num2 |= PRESET_DICT;
                }
                num2 += 0x1f - (num2 % 0x1f);
                this.statusField = BUSY_STATE;
                this.pendingField[this.pendingCountField++] = (byte) (num2 >> 8);
                this.pendingField[this.pendingCountField++] = (byte) num2;
                if (this.strstartField != 0)
                {
                    this.pendingField[this.pendingCountField++] = (byte) ((this.codecField._Adler32 & -16777216) >> 0x18);
                    this.pendingField[this.pendingCountField++] = (byte) ((this.codecField._Adler32 & 0xff0000) >> 0x10);
                    this.pendingField[this.pendingCountField++] = (byte) ((this.codecField._Adler32 & 0xff00) >> 8);
                    this.pendingField[this.pendingCountField++] = (byte) (this.codecField._Adler32 & 0xff);
                }
                this.codecField._Adler32 = Adler.Adler32(0, null, 0, 0);
            }
            if (this.pendingCountField != 0)
            {
                this.codecField.flush_pending();
                if (this.codecField.AvailableBytesOut == 0)
                {
                    this.lastFlushField = -1;
                    return 0;
                }
            }
            else if (((this.codecField.AvailableBytesIn == 0) && ((int)flush <= num)) && (flush != FlushType.Finish))
            {
                return 0;
            }
            if ((this.statusField == FINISH_STATE) && (this.codecField.AvailableBytesIn != 0))
            {
                this.codecField.Message = errorMessageField[7];
                throw new ZlibException("status == FINISH_STATE && _codec.AvailableBytesIn != 0");
            }
            if (((this.codecField.AvailableBytesIn != 0) || (this.lookAheadField != 0)) || ((flush != FlushType.None) && (this.statusField != FINISH_STATE)))
            {
                BlockState state = this.deflateFunctionField(flush);
                switch (state)
                {
                    case BlockState.FinishStarted:
                    case BlockState.FinishDone:
                        this.statusField = FINISH_STATE;
                        break;
                }
                if ((state == BlockState.NeedMore) || (state == BlockState.FinishStarted))
                {
                    if (this.codecField.AvailableBytesOut == 0)
                    {
                        this.lastFlushField = -1;
                    }
                    return 0;
                }
                if (state == BlockState.BlockDone)
                {
                    if (flush == FlushType.Partial)
                    {
                        this._tr_align();
                    }
                    else
                    {
                        this._tr_stored_block(0, 0, false);
                        if (flush == FlushType.Full)
                        {
                            for (int i = 0; i < this.hashSizeField; i++)
                            {
                                this.headField[i] = 0;
                            }
                        }
                    }
                    this.codecField.flush_pending();
                    if (this.codecField.AvailableBytesOut == 0)
                    {
                        this.lastFlushField = -1;
                        return 0;
                    }
                }
            }
            if (flush != FlushType.Finish)
            {
                return 0;
            }
            if (!(this.WantRfc1950HeaderBytes && !this.rfc1950BytesEmittedField))
            {
                return 1;
            }
            this.pendingField[this.pendingCountField++] = (byte) ((this.codecField._Adler32 & -16777216) >> 0x18);
            this.pendingField[this.pendingCountField++] = (byte) ((this.codecField._Adler32 & 0xff0000) >> 0x10);
            this.pendingField[this.pendingCountField++] = (byte) ((this.codecField._Adler32 & 0xff00) >> 8);
            this.pendingField[this.pendingCountField++] = (byte) (this.codecField._Adler32 & 0xff);
            this.codecField.flush_pending();
            this.rfc1950BytesEmittedField = true;
            return ((this.pendingCountField != 0) ? 0 : 1);
        }

        internal BlockState DeflateFast(FlushType flush)
        {
            int num = 0;
            while (true)
            {
                bool flag;
                if (this.lookAheadField < MIN_LOOKAHEAD)
                {
                    this._fillWindow();
                    if ((this.lookAheadField < MIN_LOOKAHEAD) && (flush == FlushType.None))
                    {
                        return BlockState.NeedMore;
                    }
                    if (this.lookAheadField == 0)
                    {
                        this.flush_block_only(flush == FlushType.Finish);
                        if (this.codecField.AvailableBytesOut == 0)
                        {
                            if (flush == FlushType.Finish)
                            {
                                return BlockState.FinishStarted;
                            }
                            return BlockState.NeedMore;
                        }
                        return ((flush == FlushType.Finish) ? BlockState.FinishDone : BlockState.BlockDone);
                    }
                }
                if (this.lookAheadField >= MIN_MATCH)
                {
                    this.inshField = ((this.inshField << this.hashShiftField) ^ (this.windowField[this.strstartField + (MIN_MATCH - 1)] & 0xff)) & this.hashMaskField;
                    num = this.headField[this.inshField] & 0xffff;
                    this.prevField[this.strstartField & this.wmaskField] = this.headField[this.inshField];
                    this.headField[this.inshField] = (short) this.strstartField;
                }
                if (((num != 0L) && (((this.strstartField - num) & 0xffff) <= (this.wsizeField - MIN_LOOKAHEAD))) && (this.compressionStrategyField != CompressionStrategy.HuffmanOnly))
                {
                    this.matchLengthField = this.longest_match(num);
                }
                if (this.matchLengthField >= MIN_MATCH)
                {
                    flag = this._tr_tally(this.strstartField - this.matchStartField, this.matchLengthField - MIN_MATCH);
                    this.lookAheadField -= this.matchLengthField;
                    if ((this.matchLengthField <= this.configField.MaxLazy) && (this.lookAheadField >= MIN_MATCH))
                    {
                        this.matchLengthField--;
                        do
                        {
                            this.strstartField++;
                            this.inshField = ((this.inshField << this.hashShiftField) ^ (this.windowField[this.strstartField + (MIN_MATCH - 1)] & 0xff)) & this.hashMaskField;
                            num = this.headField[this.inshField] & 0xffff;
                            this.prevField[this.strstartField & this.wmaskField] = this.headField[this.inshField];
                            this.headField[this.inshField] = (short) this.strstartField;
                        }
                        while (--this.matchLengthField != 0);
                        this.strstartField++;
                    }
                    else
                    {
                        this.strstartField += this.matchLengthField;
                        this.matchLengthField = 0;
                        this.inshField = this.windowField[this.strstartField] & 0xff;
                        this.inshField = ((this.inshField << this.hashShiftField) ^ (this.windowField[this.strstartField + 1] & 0xff)) & this.hashMaskField;
                    }
                }
                else
                {
                    flag = this._tr_tally(0, this.windowField[this.strstartField] & 0xff);
                    this.lookAheadField--;
                    this.strstartField++;
                }
                if (flag)
                {
                    this.flush_block_only(false);
                    if (this.codecField.AvailableBytesOut == 0)
                    {
                        return BlockState.NeedMore;
                    }
                }
            }
        }

        internal BlockState DeflateNone(FlushType flush)
        {
            int num = 0xffff;
            if (num > (this.pendingField.Length - 5))
            {
                num = this.pendingField.Length - 5;
            }
            while (true)
            {
                if (this.lookAheadField <= 1)
                {
                    this._fillWindow();
                    if ((this.lookAheadField == 0) && (flush == FlushType.None))
                    {
                        return BlockState.NeedMore;
                    }
                    if (this.lookAheadField == 0)
                    {
                        this.flush_block_only(flush == FlushType.Finish);
                        if (this.codecField.AvailableBytesOut == 0)
                        {
                            return ((flush == FlushType.Finish) ? BlockState.FinishStarted : BlockState.NeedMore);
                        }
                        return ((flush == FlushType.Finish) ? BlockState.FinishDone : BlockState.BlockDone);
                    }
                }
                this.strstartField += this.lookAheadField;
                this.lookAheadField = 0;
                int num2 = this.blockStartField + num;
                if ((this.strstartField == 0) || (this.strstartField >= num2))
                {
                    this.lookAheadField = this.strstartField - num2;
                    this.strstartField = num2;
                    this.flush_block_only(false);
                    if (this.codecField.AvailableBytesOut == 0)
                    {
                        return BlockState.NeedMore;
                    }
                }
                if ((this.strstartField - this.blockStartField) >= (this.wsizeField - MIN_LOOKAHEAD))
                {
                    this.flush_block_only(false);
                    if (this.codecField.AvailableBytesOut == 0)
                    {
                        return BlockState.NeedMore;
                    }
                }
            }
        }

        internal BlockState DeflateSlow(FlushType flush)
        {
            int num = 0;
            while (true)
            {
                bool flag;
                if (this.lookAheadField < MIN_LOOKAHEAD)
                {
                    this._fillWindow();
                    if ((this.lookAheadField < MIN_LOOKAHEAD) && (flush == FlushType.None))
                    {
                        return BlockState.NeedMore;
                    }
                    if (this.lookAheadField == 0)
                    {
                        if (this.matchAvailableField != 0)
                        {
                            flag = this._tr_tally(0, this.windowField[this.strstartField - 1] & 0xff);
                            this.matchAvailableField = 0;
                        }
                        this.flush_block_only(flush == FlushType.Finish);
                        if (this.codecField.AvailableBytesOut == 0)
                        {
                            if (flush == FlushType.Finish)
                            {
                                return BlockState.FinishStarted;
                            }
                            return BlockState.NeedMore;
                        }
                        return ((flush == FlushType.Finish) ? BlockState.FinishDone : BlockState.BlockDone);
                    }
                }
                if (this.lookAheadField >= MIN_MATCH)
                {
                    this.inshField = ((this.inshField << this.hashShiftField) ^ (this.windowField[this.strstartField + (MIN_MATCH - 1)] & 0xff)) & this.hashMaskField;
                    num = this.headField[this.inshField] & 0xffff;
                    this.prevField[this.strstartField & this.wmaskField] = this.headField[this.inshField];
                    this.headField[this.inshField] = (short) this.strstartField;
                }
                this.prevLengthField = this.matchLengthField;
                this.prevMatchField = this.matchStartField;
                this.matchLengthField = MIN_MATCH - 1;
                if (((num != 0) && (this.prevLengthField < this.configField.MaxLazy)) && (((this.strstartField - num) & 0xffff) <= (this.wsizeField - MIN_LOOKAHEAD)))
                {
                    if (this.compressionStrategyField != CompressionStrategy.HuffmanOnly)
                    {
                        this.matchLengthField = this.longest_match(num);
                    }
                    if ((this.matchLengthField <= 5) && ((this.compressionStrategyField == CompressionStrategy.Filtered) || ((this.matchLengthField == MIN_MATCH) && ((this.strstartField - this.matchStartField) > 0x1000))))
                    {
                        this.matchLengthField = MIN_MATCH - 1;
                    }
                }
                if ((this.prevLengthField >= MIN_MATCH) && (this.matchLengthField <= this.prevLengthField))
                {
                    int num2 = (this.strstartField + this.lookAheadField) - MIN_MATCH;
                    flag = this._tr_tally((this.strstartField - 1) - this.prevMatchField, this.prevLengthField - MIN_MATCH);
                    this.lookAheadField -= this.prevLengthField - 1;
                    this.prevLengthField -= 2;
                    do
                    {
                        if (++this.strstartField <= num2)
                        {
                            this.inshField = ((this.inshField << this.hashShiftField) ^ (this.windowField[this.strstartField + (MIN_MATCH - 1)] & 0xff)) & this.hashMaskField;
                            num = this.headField[this.inshField] & 0xffff;
                            this.prevField[this.strstartField & this.wmaskField] = this.headField[this.inshField];
                            this.headField[this.inshField] = (short) this.strstartField;
                        }
                    }
                    while (--this.prevLengthField != 0);
                    this.matchAvailableField = 0;
                    this.matchLengthField = MIN_MATCH - 1;
                    this.strstartField++;
                    if (flag)
                    {
                        this.flush_block_only(false);
                        if (this.codecField.AvailableBytesOut == 0)
                        {
                            return BlockState.NeedMore;
                        }
                    }
                }
                else if (this.matchAvailableField != 0)
                {
                    if (this._tr_tally(0, this.windowField[this.strstartField - 1] & 0xff))
                    {
                        this.flush_block_only(false);
                    }
                    this.strstartField++;
                    this.lookAheadField--;
                    if (this.codecField.AvailableBytesOut == 0)
                    {
                        return BlockState.NeedMore;
                    }
                }
                else
                {
                    this.matchAvailableField = 1;
                    this.strstartField++;
                    this.lookAheadField--;
                }
            }
        }

        internal int End()
        {
            if (((this.statusField != INIT_STATE) && (this.statusField != BUSY_STATE)) && (this.statusField != FINISH_STATE))
            {
                return -2;
            }
            this.pendingField = null;
            this.headField = null;
            this.prevField = null;
            this.windowField = null;
            return ((this.statusField == BUSY_STATE) ? -3 : 0);
        }

        internal void flush_block_only(bool eof)
        {
            this._tr_flush_block((this.blockStartField >= 0) ? this.blockStartField : -1, this.strstartField - this.blockStartField, eof);
            this.blockStartField = this.strstartField;
            this.codecField.flush_pending();
        }

        internal int Initialize(ZlibCodec codec, CompressionLevel level)
        {
            return this.Initialize(codec, level, 15);
        }

        internal int Initialize(ZlibCodec codec, CompressionLevel level, int bits)
        {
            return this.Initialize(codec, level, bits, MEM_LEVEL_DEFAULT, CompressionStrategy.Default);
        }

        internal int Initialize(ZlibCodec codec, CompressionLevel level, int bits, CompressionStrategy compressionStrategy)
        {
            return this.Initialize(codec, level, bits, MEM_LEVEL_DEFAULT, compressionStrategy);
        }

        internal int Initialize(ZlibCodec codec, CompressionLevel level, int windowBits, int memLevel, CompressionStrategy strategy)
        {
            this.codecField = codec;
            this.codecField.Message = null;
            if ((windowBits < 9) || (windowBits > 15))
            {
                throw new ZlibException("windowBits must be in the range 9..15.");
            }
            if ((memLevel < 1) || (memLevel > MEM_LEVEL_MAX))
            {
                throw new ZlibException(string.Format(CultureInfo.InvariantCulture, "memLevel must be in the range 1.. {0}", MEM_LEVEL_MAX));
            }
            this.codecField.dstate = this;
            this.wbitsField = windowBits;
            this.wsizeField = ((int) 1) << this.wbitsField;
            this.wmaskField = this.wsizeField - 1;
            this.hashBitsField = memLevel + 7;
            this.hashSizeField = ((int) 1) << this.hashBitsField;
            this.hashMaskField = this.hashSizeField - 1;
            this.hashShiftField = ((this.hashBitsField + MIN_MATCH) - 1) / MIN_MATCH;
            this.windowField = new byte[this.wsizeField * 2];
            this.prevField = new short[this.wsizeField];
            this.headField = new short[this.hashSizeField];
            this.litBufSizeField = ((int) 1) << (memLevel + 6);
            this.pendingField = new byte[this.litBufSizeField * 4];
            this.distanceOffsetField = this.litBufSizeField;
            this.lengthOffsetField = 3 * this.litBufSizeField;
            this.compressionLevelField = level;
            this.compressionStrategyField = strategy;
            this.Reset();
            return 0;
        }

        internal int longest_match(int cur_match)
        {
            int maxChainLength = this.configField.MaxChainLength;
            int strstart = this.strstartField;
            int num5 = this.prevLengthField;
            int num6 = (this.strstartField > (this.wsizeField - MIN_LOOKAHEAD)) ? (this.strstartField - (this.wsizeField - MIN_LOOKAHEAD)) : 0;
            int niceLength = this.configField.NiceLength;
            int num8 = this.wmaskField;
            int num9 = this.strstartField + MAX_MATCH;
            byte num10 = this.windowField[(strstart + num5) - 1];
            byte num11 = this.windowField[strstart + num5];
            if (this.prevLengthField >= this.configField.GoodLength)
            {
                maxChainLength = maxChainLength >> 2;
            }
            if (niceLength > this.lookAheadField)
            {
                niceLength = this.lookAheadField;
            }
            do
            {
                int index = cur_match;
                if ((((this.windowField[index + num5] == num11) && (this.windowField[(index + num5) - 1] == num10)) && (this.windowField[index] == this.windowField[strstart])) && (this.windowField[++index] == this.windowField[strstart + 1]))
                {
                    strstart += 2;
                    index++;
                    while (((((this.windowField[++strstart] == this.windowField[++index]) && (this.windowField[++strstart] == this.windowField[++index])) && ((this.windowField[++strstart] == this.windowField[++index]) && (this.windowField[++strstart] == this.windowField[++index]))) && (((this.windowField[++strstart] == this.windowField[++index]) && (this.windowField[++strstart] == this.windowField[++index])) && ((this.windowField[++strstart] == this.windowField[++index]) && (this.windowField[++strstart] == this.windowField[++index])))) && (strstart < num9))
                    {
                    }
                    int num4 = MAX_MATCH - (num9 - strstart);
                    strstart = num9 - MAX_MATCH;
                    if (num4 > num5)
                    {
                        this.matchStartField = cur_match;
                        num5 = num4;
                        if (num4 >= niceLength)
                        {
                            break;
                        }
                        num10 = this.windowField[(strstart + num5) - 1];
                        num11 = this.windowField[strstart + num5];
                    }
                }
            }
            while (((cur_match = this.prevField[cur_match & num8] & 0xffff) > num6) && (--maxChainLength != 0));
            if (num5 <= this.lookAheadField)
            {
                return num5;
            }
            return this.lookAheadField;
        }

        internal void pqdownheap(short[] tree, int k)
        {
            int n = this.heapField[k];
            for (int i = k << 1; i <= this.heapLenField; i = i << 1)
            {
                if ((i < this.heapLenField) && _IsSmaller(tree, this.heapField[i + 1], this.heapField[i], this.depthField))
                {
                    i++;
                }
                if (_IsSmaller(tree, n, this.heapField[i], this.depthField))
                {
                    break;
                }
                this.heapField[k] = this.heapField[i];
                k = i;
            }
            this.heapField[k] = n;
        }

        private void put_bytes(byte[] p, int start, int len)
        {
            Array.Copy(p, start, this.pendingField, this.pendingCountField, len);
            this.pendingCountField += len;
        }

        internal void Reset()
        {
            this.codecField.TotalBytesIn = this.codecField.TotalBytesOut = 0L;
            this.codecField.Message = null;
            this.pendingCountField = 0;
            this.nextPendingField = 0;
            this.rfc1950BytesEmittedField = false;
            this.statusField = this.WantRfc1950HeaderBytes ? INIT_STATE : BUSY_STATE;
            this.codecField._Adler32 = Adler.Adler32(0, null, 0, 0);
            this.lastFlushField = 0;
            this._InitializeTreeData();
            this._InitializeLazyMatch();
        }

        internal void scan_tree(short[] tree, int max_code)
        {
            int num2 = -1;
            int num4 = tree[1];
            int num5 = 0;
            int num6 = 7;
            int num7 = 4;
            if (num4 == 0)
            {
                num6 = 0x8a;
                num7 = 3;
            }
            tree[((max_code + 1) * 2) + 1] = 0x7fff;
            for (int i = 0; i <= max_code; i++)
            {
                int num3 = num4;
                num4 = tree[((i + 1) * 2) + 1];
                if ((++num5 >= num6) || (num3 != num4))
                {
                    if (num5 < num7)
                    {
                        this.bltreeField[num3 * 2] = (short) (this.bltreeField[num3 * 2] + num5);
                    }
                    else if (num3 != 0)
                    {
                        if (num3 != num2)
                        {
                            this.bltreeField[num3 * 2] = (short) (this.bltreeField[num3 * 2] + 1);
                        }
                        this.bltreeField[InternalConstants.REP_3_6 * 2] = (short) (this.bltreeField[InternalConstants.REP_3_6 * 2] + 1);
                    }
                    else if (num5 <= 10)
                    {
                        this.bltreeField[InternalConstants.REPZ_3_10 * 2] = (short) (this.bltreeField[InternalConstants.REPZ_3_10 * 2] + 1);
                    }
                    else
                    {
                        this.bltreeField[InternalConstants.REPZ_11_138 * 2] = (short) (this.bltreeField[InternalConstants.REPZ_11_138 * 2] + 1);
                    }
                    num5 = 0;
                    num2 = num3;
                    if (num4 == 0)
                    {
                        num6 = 0x8a;
                        num7 = 3;
                    }
                    else if (num3 == num4)
                    {
                        num6 = 6;
                        num7 = 3;
                    }
                    else
                    {
                        num6 = 7;
                        num7 = 4;
                    }
                }
            }
        }

        internal void send_all_trees(int lcodes, int dcodes, int blcodes)
        {
            this.send_bits(lcodes - 0x101, 5);
            this.send_bits(dcodes - 1, 5);
            this.send_bits(blcodes - 4, 4);
            for (int i = 0; i < blcodes; i++)
            {
                this.send_bits(this.bltreeField[(Tree.bl_order[i] * 2) + 1], 3);
            }
            this.send_tree(this.dynLtreeField, lcodes - 1);
            this.send_tree(this.dynDtreeField, dcodes - 1);
        }

        internal void send_bits(int value, int length)
        {
            int num = length;
            if (this.bivalidField > (Buf_size - num))
            {
                this.bibufField = (short) (this.bibufField | ((short) ((value << this.bivalidField) & 0xffff)));
                this.pendingField[this.pendingCountField++] = (byte) this.bibufField;
                this.pendingField[this.pendingCountField++] = (byte) (this.bibufField >> 8);
                this.bibufField = (short) (value >> (Buf_size - this.bivalidField));
                this.bivalidField += num - Buf_size;
            }
            else
            {
                this.bibufField = (short) (this.bibufField | ((short) ((value << this.bivalidField) & 0xffff)));
                this.bivalidField += num;
            }
        }

        internal void send_code(int c, short[] tree)
        {
            int index = c * 2;
            this.send_bits(tree[index] & 0xffff, tree[index + 1] & 0xffff);
        }

        internal void send_compressed_block(short[] ltree, short[] dtree)
        {
            int num3 = 0;
            if (this.lastLitField != 0)
            {
                do
                {
                    int index = this.distanceOffsetField + (num3 * 2);
                    int dist = ((this.pendingField[index] << 8) & 0xff00) | (this.pendingField[index + 1] & 0xff);
                    int c = this.pendingField[this.lengthOffsetField + num3] & 0xff;
                    num3++;
                    if (dist == 0)
                    {
                        this.send_code(c, ltree);
                    }
                    else
                    {
                        int num4 = Tree.LengthCode[c];
                        this.send_code((num4 + InternalConstants.LITERALS) + 1, ltree);
                        int length = Tree.ExtraLengthBits[num4];
                        if (length != 0)
                        {
                            c -= Tree.LengthBase[num4];
                            this.send_bits(c, length);
                        }
                        dist--;
                        num4 = Tree.DistanceCode(dist);
                        this.send_code(num4, dtree);
                        length = Tree.ExtraDistanceBits[num4];
                        if (length != 0)
                        {
                            dist -= Tree.DistanceBase[num4];
                            this.send_bits(dist, length);
                        }
                    }
                }
                while (num3 < this.lastLitField);
            }
            this.send_code(END_BLOCK, ltree);
            this.lastEobLenField = ltree[(END_BLOCK * 2) + 1];
        }

        internal void send_tree(short[] tree, int max_code)
        {
            int num2 = -1;
            int num4 = tree[1];
            int num5 = 0;
            int num6 = 7;
            int num7 = 4;
            if (num4 == 0)
            {
                num6 = 0x8a;
                num7 = 3;
            }
            for (int i = 0; i <= max_code; i++)
            {
                int c = num4;
                num4 = tree[((i + 1) * 2) + 1];
                if ((++num5 >= num6) || (c != num4))
                {
                    if (num5 < num7)
                    {
                        do
                        {
                            this.send_code(c, this.bltreeField);
                        }
                        while (--num5 != 0);
                    }
                    else if (c != 0)
                    {
                        if (c != num2)
                        {
                            this.send_code(c, this.bltreeField);
                            num5--;
                        }
                        this.send_code(InternalConstants.REP_3_6, this.bltreeField);
                        this.send_bits(num5 - 3, 2);
                    }
                    else if (num5 <= 10)
                    {
                        this.send_code(InternalConstants.REPZ_3_10, this.bltreeField);
                        this.send_bits(num5 - 3, 3);
                    }
                    else
                    {
                        this.send_code(InternalConstants.REPZ_11_138, this.bltreeField);
                        this.send_bits(num5 - 11, 7);
                    }
                    num5 = 0;
                    num2 = c;
                    if (num4 == 0)
                    {
                        num6 = 0x8a;
                        num7 = 3;
                    }
                    else if (c == num4)
                    {
                        num6 = 6;
                        num7 = 3;
                    }
                    else
                    {
                        num6 = 7;
                        num7 = 4;
                    }
                }
            }
        }

        internal void set_data_type()
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            while (num < 7)
            {
                num3 += this.dynLtreeField[num * 2];
                num++;
            }
            while (num < 0x80)
            {
                num2 += this.dynLtreeField[num * 2];
                num++;
            }
            while (num < InternalConstants.LITERALS)
            {
                num3 += this.dynLtreeField[num * 2];
                num++;
            }
            this.dataTypeField = (num3 > (num2 >> 2)) ? ((sbyte) Z_BINARY) : ((sbyte) Z_ASCII);
        }

        private void SetDeflater()
        {
            switch (this.configField.Flavor)
            {
                case DeflateFlavor.Store:
                    this.deflateFunctionField = new CompressFunc(this.DeflateNone);
                    break;

                case DeflateFlavor.Fast:
                    this.deflateFunctionField = new CompressFunc(this.DeflateFast);
                    break;

                case DeflateFlavor.Slow:
                    this.deflateFunctionField = new CompressFunc(this.DeflateSlow);
                    break;
            }
        }

        internal int SetDictionary(byte[] dictionary)
        {
            int length = dictionary.Length;
            int sourceIndex = 0;
            if ((dictionary == null) || (this.statusField != INIT_STATE))
            {
                throw new ZlibException("Stream error.");
            }
            this.codecField._Adler32 = Adler.Adler32(this.codecField._Adler32, dictionary, 0, dictionary.Length);
            if (length >= MIN_MATCH)
            {
                if (length > (this.wsizeField - MIN_LOOKAHEAD))
                {
                    length = this.wsizeField - MIN_LOOKAHEAD;
                    sourceIndex = dictionary.Length - length;
                }
                Array.Copy(dictionary, sourceIndex, this.windowField, 0, length);
                this.strstartField = length;
                this.blockStartField = length;
                this.inshField = this.windowField[0] & 0xff;
                this.inshField = ((this.inshField << this.hashShiftField) ^ (this.windowField[1] & 0xff)) & this.hashMaskField;
                for (int i = 0; i <= (length - MIN_MATCH); i++)
                {
                    this.inshField = ((this.inshField << this.hashShiftField) ^ (this.windowField[i + (MIN_MATCH - 1)] & 0xff)) & this.hashMaskField;
                    this.prevField[i & this.wmaskField] = this.headField[this.inshField];
                    this.headField[this.inshField] = (short) i;
                }
            }
            return 0;
        }

        internal int SetParams(CompressionLevel level, CompressionStrategy strategy)
        {
            int num = 0;
            if (this.compressionLevelField != level)
            {
                Config config = Config.Lookup(level);
                if ((config.Flavor != this.configField.Flavor) && (this.codecField.TotalBytesIn != 0L))
                {
                    num = this.codecField.Deflate(FlushType.Partial);
                }
                this.compressionLevelField = level;
                this.configField = config;
                this.SetDeflater();
            }
            this.compressionStrategyField = strategy;
            return num;
        }

        internal bool WantRfc1950HeaderBytes
        {
            get
            {
                return this.wantRfc1950HeaderBytesField;
            }
            set
            {
                this.wantRfc1950HeaderBytesField = value;
            }
        }

        internal delegate BlockState CompressFunc(FlushType flush);

        internal class Config
        {
            internal DeflateFlavor Flavor;
            internal int GoodLength;
            internal int MaxChainLength;
            internal int MaxLazy;
            internal int NiceLength;
            private static readonly DeflateManager.Config[] Table = new DeflateManager.Config[] { new DeflateManager.Config(0, 0, 0, 0, DeflateFlavor.Store), new DeflateManager.Config(4, 4, 8, 4, DeflateFlavor.Fast), new DeflateManager.Config(4, 5, 0x10, 8, DeflateFlavor.Fast), new DeflateManager.Config(4, 6, 0x20, 0x20, DeflateFlavor.Fast), new DeflateManager.Config(4, 4, 0x10, 0x10, DeflateFlavor.Slow), new DeflateManager.Config(8, 0x10, 0x20, 0x20, DeflateFlavor.Slow), new DeflateManager.Config(8, 0x10, 0x80, 0x80, DeflateFlavor.Slow), new DeflateManager.Config(8, 0x20, 0x80, 0x100, DeflateFlavor.Slow), new DeflateManager.Config(0x20, 0x80, 0x102, 0x400, DeflateFlavor.Slow), new DeflateManager.Config(0x20, 0x102, 0x102, 0x1000, DeflateFlavor.Slow) };

            private Config(int goodLength, int maxLazy, int niceLength, int maxChainLength, DeflateFlavor flavor)
            {
                this.GoodLength = goodLength;
                this.MaxLazy = maxLazy;
                this.NiceLength = niceLength;
                this.MaxChainLength = maxChainLength;
                this.Flavor = flavor;
            }

            public static DeflateManager.Config Lookup(CompressionLevel level)
            {
                return Table[(int) level];
            }
        }
    }
}

