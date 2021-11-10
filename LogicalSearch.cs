using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    public class LogicalSearch
    {
        public Node _root;
        public string _searchQuery;
        public bool _matchCase;


        // Expression tree constructor
        public LogicalSearch(string searchQuery)
        {
            _searchQuery = searchQuery;

            Stack st = new Stack();
            Node t, t1, t2;

            // get an array containing only the search queries (without logical operators)
            string[] expressionArray = searchQuery.Split(new string[] { "AND", "OR", "&&", "||" }, StringSplitOptions.RemoveEmptyEntries);
            for (int ii = 0; ii < expressionArray.Length; ii++)
                expressionArray[ii] = expressionArray[ii].Trim();

            // get postfix expression in single char form for building the expression tree
            string postfix = inToPost(toSingleCharForm(searchQuery.Split(' ')));

            // traverse through every character of input expression
            for (int i = 0; i < postfix.Length; i++)
            {
                if (!isOperator(postfix[i]))
                {
                    // if operand, push into stack
                    t = new Node(postfix[i], expressionArray);
                    st.Push(t);
                }
                else
                {
                    // if operator, create a node
                    t = new Node(postfix[i], expressionArray);

                    // pop off the two top nodes and make them children
                    t1 = (Node)st.Pop();
                    t2 = (Node)st.Pop();
                    t.right = t1;
                    t.left = t2;

                    // add this subexpression to stack
                    st.Push(t);
                }
            }

            // only element will be root of expression tree
            t = (Node)st.Peek();
            st.Pop();

            // set root node field for this expression tree
            _root = t;
        }


        ///<summary>
        ///Evaluates the inputted line against the calling instance of LogicalSearch with the option of case-sensitivity. Returns true if the line evaluates to true against the expression tree.
        ///</summary>
        public bool Evaluate(string line, bool matchCase)
        {
            bool ret = true;
            evaluateRecursive(line, matchCase, _root, ref ret);
            return ret;
        }

        ///<summary>
        ///Evaluates the inputted file (from FileInfo) against the calling instance of LogicalSearch with the option of case-sensitivity. Returns a list of SearchResults objects containing matching lines and their line numbers in the file.
        ///</summary>
        public List<SearchResults> Evaluate(FileInfo file, bool matchCase = false)
        {
            List<SearchResults> ret = new List<SearchResults>();
            string lineRead = "";
            int idx = 1;

            // loop through file
            using (var reader = new StreamReader(file.FullName))
            {
                while ((lineRead = reader.ReadLine()) != null)
                {
                    // if a match is found, add to SearchResults
                    if (Evaluate(lineRead, matchCase))
                    {
                        SearchResults temp = new SearchResults(lineRead, idx);                        
                        ret.Add(temp);
                    }

                    idx++;
                }
            }

            return ret;
        }


        ///<summary>
        ///Evaluates the list of inputted files against the calling instance of LogicalSearch with the options of file extension and case-sensitivity. Returns a list of SearchResults objects containing matching lines and their line numbers in the file.
        ///To search only files of certain extensions, pass a string array containing the extensions (with periods) that you are interested in to this method. 
        ///</summary>
        public List<SearchResults> Evaluate(DirectoryInfo directory, bool matchCase = false, string[] extensions = null)
        {
            List<SearchResults> ret = new List<SearchResults>();
            string lineRead = "";           
            var files = Directory.GetFiles(directory.FullName);
            int idx = 1;            

            // loop through files
            foreach (var file in files)
            {
                // only process requested file types
                if (extensions != null && !extensions.Contains(Path.GetExtension(file)))
                    continue;

                // read file line by line
                using (var reader = new StreamReader(file))
                {
                    while ((lineRead = reader.ReadLine()) != null)
                    {
                        // if a match is found, add to SearchResults
                        if (Evaluate(lineRead, matchCase))
                        {
                            SearchResults temp = new SearchResults(lineRead, idx, new FileInfo(file));
                            ret.Add(temp);
                        }

                        idx++;
                    }
                    idx = 1;
                }
            }

            return ret;
        }


        // traverse the tree and when at operator, check if the search string
        // matches the child nodes based on the operator (AND or OR)
        // ret is false if no match. if match, ret is true.
        private void evaluateRecursive(string line, bool matchCase, Node t, ref bool ret)
        {
            if (t != null)
            {
                evaluateRecursive(line, matchCase, t.left, ref ret);

                if (t.value == '&') // current node is AND operator
                {
                    if (!isOperator(t.left.value) && !isOperator(t.right.value))
                    {
                        // both children are search terms
                        if (!(ToLower(line, matchCase).Contains(ToLower(t.left?.realVal, matchCase))) ||
                            (!(ToLower(line, matchCase).Contains(ToLower(t.right?.realVal, matchCase)))))
                        {
                            ret = false;
                        }
                    }
                    else if (isOperator(t.left.value)) 
                    {
                        if (ret)
                        {
                            if (!ToLower(line, matchCase).Contains(ToLower(t.right?.realVal, matchCase)))
                                ret = false;
                        }
                    }
                    else if (isOperator(t.right.value))
                    {
                        if (ret)
                        {
                            if (!ToLower(line, matchCase).Contains(ToLower(t.left?.realVal, matchCase)))
                                ret = false;
                        }
                    }
                }
                else if (t.value == '|') // current node is OR operator
                {
                    if (!isOperator(t.left.value) && !isOperator(t.right.value))
                    {
                        // both children are search terms
                        if (!(ToLower(line, matchCase).Contains(ToLower(t.left?.realVal, matchCase))) &&
                            (!(ToLower(line, matchCase).Contains(ToLower(t.right?.realVal, matchCase)))))
                        {
                            ret = false;
                        }
                    }
                    else if (isOperator(t.left.value))
                    {
                        if (!ret)
                        {
                            if (ToLower(line, matchCase).Contains(ToLower(t.right?.realVal, matchCase)))
                                ret = true;
                        }
                    }
                    else if (isOperator(t.right.value))
                    {
                        if (!ret)
                        {
                            if (ToLower(line, matchCase).Contains(ToLower(t.left?.realVal, matchCase)))
                                ret = true;
                        }
                    }
                }

                evaluateRecursive(line, matchCase, t.right, ref ret);
            }
        }


        // returns true if c is an operator 
        static bool isOperator(char c)
        {
            if (c == '&' || c == '|')
                return true;

            return false;
        }


        // return s as lower case if matchCase is false
        static string ToLower(string s, bool matchCase)
        {
            if (matchCase)
                return s;
            else
            {
                return s.ToLower();
            }
        }


        // convert an expression in infix form into postfix form
        static string inToPost(string infix)
        {
            Stack<char> stk = new Stack<char>();

            stk.Push('#');  // add some extra characters to avoid underflow
            string postfix = ""; // initialize output string

            for (int i = 0; i < infix.Length; i++)
            {
                if (char.IsLetterOrDigit(infix[i]))
                    postfix += infix[i];     // add to postfix expression when character is letter or number
                else if (infix[i] == '(')
                    stk.Push('(');
                else if (infix[i] == ')')
                {
                    while (stk.Peek() != '#' && stk.Peek() != '(')
                    {
                        // add to postfix expression until "(" is found
                        postfix += stk.Peek(); 
                        stk.Pop();
                    }
                    stk.Pop();  // remove '(' from the stack
                }
                else
                {
                    if (preced(infix[i]) > preced(stk.Peek()))
                        stk.Push(infix[i]); //push if precedence is high
                    else
                    {
                        while (stk.Peek() != '#' && preced(infix[i]) <= preced(stk.Peek()))
                        {
                            // add characters to the postfix expression from the top
                            // of the stack until a character of higher precedence is found
                            postfix += stk.Peek();
                            stk.Pop();
                        }
                        stk.Push(infix[i]);
                    }
                }
            }

            while (stk.Peek() != '#')
            {
                // add characters to the postfix expression from the top of the stack until empty
                postfix += stk.Peek();        
                stk.Pop();
            }

            return postfix;
        }


        // return the precedence of an operator
        static int preced(char ch)
        {
            if (ch == '&')
                return 2;
            else if (ch == '|')
                return 1;
            else
                return 0;
        }


        // convert search query expression from string form to character form
        static string toSingleCharForm(string[] stringForm)
        {
            string result = "";
            string word = "";
            int i = 0;

            foreach (string s in stringForm)
            {
                if (s != "AND" && s != "OR" && s != "&&" && s != "||")
                {
                    word = word + s + " ";
                }
                else
                {
                    word = "";
                    result += i++;

                    if (s == "AND" || s == "&&")
                        result += '&';
                    else if (s == "OR" || s == "||")
                        result += '|';
                }
            }

            result += i++;

            return result;
        }
    }


    public class SearchResults
    {
        public SearchResults()
        {
            Result = "";            
            LineNumber = -1;
            File = new FileInfo("");
        }        

        public SearchResults(string result, int lineNumber, FileInfo file = null)
        {
            Result = result;
            LineNumber = lineNumber;
            File = file;
        }

        private string _result;
        public string Result
        {
            get { return _result; }
            set { _result = value; }
        }

        private int _lineNumber;
        public int LineNumber
        {
            get { return _lineNumber; }
            set { _lineNumber = value; }
        }

        private FileInfo _file;
        public FileInfo File
        {
            get { return _file; }
            set { _file = value; }
        }
    }


    public class Node
    {
        public char value;
        public string realVal;
        public Node left, right;


        public Node(char item, string[] original)
        {
            // convert to search term
            if (char.IsNumber(item))
            {
                realVal = original[int.Parse(item.ToString())];
            }
            else // converts to AND or OR
            {
                if (item == '&')
                    realVal = "AND";
                else if (item == '|')
                    realVal = "OR";
            }

            value = item;
            left = right = null;
        }


        public bool HasNoOperatorChildren()
        {
            if (left.value != '&' && left.value != '|' && right.value != '&' && right.value != '|')
                return true;
            else
                return false;
        }
    }
}
