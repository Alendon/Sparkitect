import os

def compose_files(directories, output_file, header):
    with open(output_file, 'w', encoding='utf-8') as outfile:
        # Write the header
        outfile.write(header + "\n\n")

        # Process each directory
        for directory in directories:
            for root, _, files in os.walk(directory):
                for file in files:
                    if file.endswith('.md'):  # You can modify this to include other file types
                        file_path = os.path.join(root, file)
                        relative_path = os.path.relpath(file_path, directory)
                        
                        # Write the relative path
                        outfile.write(f"%%% File: {relative_path} %%%\n\n")
                        
                        # Write the content of the file
                        try:
                            with open(file_path, 'r', encoding='utf-8') as infile:
                                outfile.write(infile.read())
                            outfile.write("\n\n")  # Add some space between files
                        except Exception as e:
                            outfile.write(f"Error reading file: {str(e)}\n\n")

if __name__ == "__main__":
    # List of directories to process
    directories = [
        "./concepts",
        "./manual",
    ]

    # Output file path
    output_file = "export.txt"

    # Header text
    header = """
    Export of the Sparkitect documentation. API documentation is not included
    """
    
    # Delete the output file if it exists
    if os.path.exists(output_file):
        os.remove(output_file)

    compose_files(directories, output_file, header)
    print(f"Files have been composed into {output_file}")
